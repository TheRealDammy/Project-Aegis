using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Generates contract offers from ContractTemplateSO data.
/// Manages offer pool, contract acceptance per DD-12 and DD-14,
/// and contract delivery/resolution per DD-09.
/// </summary>
public class ContractManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<Contract> OnContractOffered;
    public static event Action<Contract> OnContractAccepted;
    public static event Action<Contract> OnContractCompleted;
    public static event Action<Contract> OnContractFailed;
    public static event Action<IReadOnlyList<Contract>> OnOffersUpdated;
    public static event Action<IReadOnlyList<Contract>> OnActiveContractsUpdated;

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private ContractTemplateSO[] _contractTemplates;
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private EmployeeManager _employeeManager;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<Contract> AvailableContracts => _availableContracts;
    public IReadOnlyList<Contract> ActiveContracts => _activeContracts;

    // — Private State —————————————————————————————————————
    private readonly List<Contract> _availableContracts = new();
    private readonly List<Contract> _activeContracts = new();
    private readonly HashSet<string> _completedNodeIds = new();

    // Reputation tier — hardcoded to 1. QA-004 pattern.
    // Wired to ReputationManager.OnTierChanged in M3.
    private int _playerRepTier = 1;

    private int _contractIdCounter = 0;

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        SeedCompletedResearch();
        TopUpOfferPool();
    }

    private void OnEnable()
    {
        TimeManager.OnWeekTick += HandleWeekTick;
        ResearchManager.OnResearchCompleted += HandleResearchCompleted;
        ReputationManager.OnTierChanged += HandleTierChanged;
    }

    private void OnDisable()
    {
        TimeManager.OnWeekTick -= HandleWeekTick;
        ResearchManager.OnResearchCompleted -= HandleResearchCompleted;
        ReputationManager.OnTierChanged -= HandleTierChanged;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Accepts a contract offer per DD-12 and DD-14.
    /// Risk is calculated immediately from the provided engineer list and locked permanently.
    /// Engineers are marked as assigned. Contract moves to active pool.
    /// </summary>
    public bool AcceptContract(Contract contract, IReadOnlyList<Employee> assignedEngineers)
    {
        if (!_availableContracts.Contains(contract))
        {
            Debug.LogWarning($"[ContractManager] AcceptContract: '{contract.ContractId}' " +
                             "not in available pool.");
            return false;
        }

        // DD-12: lock risk at acceptance. Never recalculates.
        contract.LockedSuccessChance = CalculateSuccessChance(
            assignedEngineers,
            requiredEngineerCount: 1,   // M3: 1 required. Full staffing model is M4.
            contractReputationTier: contract.ReputationTierRequired,
            budgetAllocated: contract.BudgetAllocated,
            contractBaseCost: contract.BaseCostGBP);

        contract.LockedEngineerCount = assignedEngineers.Count;
        contract.IsActive = true;

        // Assign engineers — marked so they don't double-book on other contracts.
        foreach (Employee eng in assignedEngineers)
        {
            eng.Assignment = contract.ContractId;
            contract.AssignedEmployeeIds.Add(eng.EmployeeId);   // Was: AssignedEmployeeNames, eng.Name
        }

        _availableContracts.Remove(contract);
        _activeContracts.Add(contract);

        OnContractAccepted?.Invoke(contract);
        OnActiveContractsUpdated?.Invoke(_activeContracts);

        // Replace the accepted offer immediately so pool stays at target size.
        TopUpOfferPool();

        Debug.Log($"[ContractManager] Accepted: {contract.ContractId} | " +
                  $"Risk: {contract.LockedSuccessChance:F1}% | " +
                  $"Engineers: {assignedEngineers.Count} | " +
                  $"Deadline: {contract.DeadlineWeeks} weeks.");
        return true;
    }

    /// <summary>Calculates contract success chance per DD-09. See formula in ContractManager.cs.</summary>
    public float CalculateSuccessChance(
        IReadOnlyList<Employee> assignedEngineers,
        int requiredEngineerCount,
        int contractReputationTier,
        float budgetAllocated,
        float contractBaseCost)
    {
        if (assignedEngineers == null || assignedEngineers.Count == 0)
            return AegisConstants.MIN_CONTRACT_CHANCE;

        float teamBonus = CalculateTeamBonus(assignedEngineers, requiredEngineerCount);
        float complexityPenalty = CalculateComplexityPenalty(contractReputationTier);
        float budgetBonus = CalculateBudgetBonus(budgetAllocated, contractBaseCost);

        float raw = AegisConstants.BASE_CONTRACT_CHANCE + teamBonus + budgetBonus - complexityPenalty;
        return Mathf.Clamp(raw, AegisConstants.MIN_CONTRACT_CHANCE, AegisConstants.MAX_CONTRACT_CHANCE);
    }

    public void PopulateSaveData(GameSaveData data)
    {
        data.ContractIdCounter = _contractIdCounter;
        data.AvailableContracts = ContractsToSaveData(_availableContracts);
        data.ActiveContracts = ContractsToSaveData(_activeContracts);
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        _contractIdCounter = data.ContractIdCounter;
        _availableContracts.Clear();
        _activeContracts.Clear();

        if (data.AvailableContracts != null)
            foreach (ContractSaveData d in data.AvailableContracts)
                _availableContracts.Add(SaveDataToContract(d));

        if (data.ActiveContracts != null)
            foreach (ContractSaveData d in data.ActiveContracts)
                _activeContracts.Add(SaveDataToContract(d));

        // Re-seed completed research from ResearchManager's restored state.
        // ResearchManager must be loaded before ContractManager.
        _completedNodeIds.Clear();
        SeedCompletedResearch();

        OnOffersUpdated?.Invoke(_availableContracts);
        OnActiveContractsUpdated?.Invoke(_activeContracts);

        Debug.Log($"[ContractManager] Loaded. Available: {_availableContracts.Count}, " +
                  $"Active: {_activeContracts.Count}. ID counter: {_contractIdCounter}.");
    }

    // — Private: Tick —————————————————————————————————————

    private void HandleWeekTick()
    {
        TickActiveContracts();
        TopUpOfferPool();
    }

    private void TickActiveContracts()
    {
        var toResolve = new List<Contract>();

        foreach (Contract contract in _activeContracts)
        {
            contract.WeeksRemaining--;
            if (contract.WeeksRemaining <= 0)
                toResolve.Add(contract);
        }

        foreach (Contract contract in toResolve)
            ResolveContract(contract);

        if (toResolve.Count > 0)
            OnActiveContractsUpdated?.Invoke(_activeContracts);
    }

    private void ResolveContract(Contract contract)
    {
        _activeContracts.Remove(contract);

        // Roll against the locked success chance.
        bool success = Random.Range(0f, 100f) < contract.LockedSuccessChance;

        if (success)
        {
            OnContractCompleted?.Invoke(contract);
            Debug.Log($"[ContractManager] SUCCESS: {contract.ContractId} — " +
                      $"£{contract.BaseRewardGBP:N0} earned.");
        }
        else
        {
            OnContractFailed?.Invoke(contract);
            Debug.Log($"[ContractManager] FAILED: {contract.ContractId} — " +
                      $"Penalty: £{contract.BaseCostGBP * AegisConstants.CONTRACT_FAILURE_PENALTY_RATIO:N0}.");
        }

        // Unassign engineers. ContractManager owns this step because it
        // has both the contract's assigned list and the EmployeeManager reference.
        FreeAssignedEngineers(contract);
    }

    private void FreeAssignedEngineers(Contract contract)
    {
        if (_employeeManager == null) return;

        foreach (string id in contract.AssignedEmployeeIds)   // Was: AssignedEmployeeNames
        {
            Employee emp = _employeeManager.GetEmployeeById(id);   // Was: GetEmployeeByName
            if (emp != null)
                emp.Assignment = null;
        }
    }

    // — Private: Events ————————————————————————————————————

    private void HandleResearchCompleted(ResearchNodeSO node)
    {
        _completedNodeIds.Add(node.NodeId);
        TopUpOfferPool(); // New tech may unlock previously filtered templates.
    }

    private void HandleTierChanged(int newTier)
    {
        _playerRepTier = newTier;
        TopUpOfferPool(); // Higher tier may unlock new contract categories.
    }

    // — Private: Offer Pool ————————————————————————————————

    private void SeedCompletedResearch()
    {
        if (_researchManager == null) return;
        foreach (var kvp in _researchManager.NodeStates)
            if (kvp.Value == ResearchNodeState.Complete)
                _completedNodeIds.Add(kvp.Key);
    }

    private void TopUpOfferPool()
    {
        if (_contractTemplates == null || _contractTemplates.Length == 0) return;

        List<ContractTemplateSO> eligible = GetEligibleTemplates();
        if (eligible.Count == 0) return;

        while (_availableContracts.Count < AegisConstants.CONTRACT_POOL_SIZE)
        {
            ContractTemplateSO template = eligible[Random.Range(0, eligible.Count)];
            Contract contract = GenerateFromTemplate(template);
            _availableContracts.Add(contract);
            OnContractOffered?.Invoke(contract);
        }

        OnOffersUpdated?.Invoke(_availableContracts);
    }

    private List<ContractTemplateSO> GetEligibleTemplates()
    {
        var eligible = new List<ContractTemplateSO>();
        foreach (ContractTemplateSO template in _contractTemplates)
        {
            if (template == null) continue;
            if (template.MinReputationTier > _playerRepTier) continue;
            if (template.RequiredResearch != null &&
                !_completedNodeIds.Contains(template.RequiredResearch.NodeId)) continue;
            eligible.Add(template);
        }
        return eligible;
    }

    private Contract GenerateFromTemplate(ContractTemplateSO template)
    {
        float normalisedRep = (_playerRepTier - 1) / 4f;
        float rewardMultiplier = template.RewardScaleByReputation != null
            ? template.RewardScaleByReputation.Evaluate(normalisedRep)
            : 1f;

        return new Contract
        {
            ContractId = $"CON_{++_contractIdCounter:D4}",
            ClientRegion = "Unknown Region",
            ContractCategory = template.ContractCategory,
            ReputationTierRequired = template.MinReputationTier,
            BaseRewardGBP = template.BaseRewardGBP * rewardMultiplier,
            BaseCostGBP = template.BaseRewardGBP * 0.3f,
            DeadlineWeeks = template.BaseDeadlineWeeks,
            WeeksRemaining = template.BaseDeadlineWeeks,
            BudgetAllocated = 0f,
            IsActive = false,
            LockedSuccessChance = 0f,
            LockedEngineerCount = 0
        };
    }

    // — Private: Risk Formula ——————————————————————————————

    private float CalculateTeamBonus(IReadOnlyList<Employee> engineers, int requiredCount)
    {
        float staffingFactor = Mathf.Min(1f, (float)engineers.Count / requiredCount);
        float total = 0f;
        foreach (Employee eng in engineers)
        {
            total +=
                eng.GetModifiedStat(AegisConstants.STAT_EFFICIENCY) * 0.5f +
                eng.GetModifiedStat(AegisConstants.STAT_INTELLIGENCE) * 0.3f +
                eng.GetModifiedStat(AegisConstants.STAT_CREATIVITY) * 0.2f;
        }
        return ((total / engineers.Count) * staffingFactor - 50f)
               * AegisConstants.TEAM_BONUS_MULTIPLIER;
    }

    private static float CalculateComplexityPenalty(int tier) =>
        (tier - 1) * AegisConstants.COMPLEXITY_PENALTY_PER_TIER;

    private float CalculateBudgetBonus(float allocated, float baseCost)
    {
        if (baseCost <= 0f) return 0f;
        return Mathf.Clamp((allocated / baseCost - 1f) * AegisConstants.MAX_BUDGET_BONUS,
                            0f, AegisConstants.MAX_BUDGET_BONUS);
    }

    // — Private: Save Data Conversion ——————————————————————
    private static List<ContractSaveData> ContractsToSaveData(List<Contract> contracts)
    {
        var list = new List<ContractSaveData>();
        foreach (Contract c in contracts)
        {
            list.Add(new ContractSaveData
            {
                ContractId = c.ContractId,
                ClientRegion = c.ClientRegion,
                ContractCategory = c.ContractCategory,
                ReputationTierRequired = c.ReputationTierRequired,
                BaseRewardGBP = c.BaseRewardGBP,
                BaseCostGBP = c.BaseCostGBP,
                DeadlineWeeks = c.DeadlineWeeks,
                WeeksRemaining = c.WeeksRemaining,
                BudgetAllocated = c.BudgetAllocated,
                IsActive = c.IsActive,
                AssignedEmployeeIds = new List<string>(c.AssignedEmployeeIds),
                LockedSuccessChance = c.LockedSuccessChance,
                LockedEngineerCount = c.LockedEngineerCount
            });
        }
        return list;
    }

    private static Contract SaveDataToContract(ContractSaveData d)
    {
        return new Contract
        {
            ContractId = d.ContractId,
            ClientRegion = d.ClientRegion,
            ContractCategory = d.ContractCategory,
            ReputationTierRequired = d.ReputationTierRequired,
            BaseRewardGBP = d.BaseRewardGBP,
            BaseCostGBP = d.BaseCostGBP,
            DeadlineWeeks = d.DeadlineWeeks,
            WeeksRemaining = d.WeeksRemaining,
            BudgetAllocated = d.BudgetAllocated,
            IsActive = d.IsActive,
            AssignedEmployeeIds = new List<string>(d.AssignedEmployeeIds ?? new List<string>()),
            LockedSuccessChance = d.LockedSuccessChance,
            LockedEngineerCount = d.LockedEngineerCount
        };
    }
}
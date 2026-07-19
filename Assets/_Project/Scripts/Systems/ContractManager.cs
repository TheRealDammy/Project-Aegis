using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages contract offers, acceptance, engineer assignment, and delivery.
///
/// Flow per DD-12 (updated):
///   Accept  — commitment locks. Contract moves to active pool. No engineers yet.
///   Assign  — player assigns engineers via EMP panel. Countdown starts.
///   Stall   — countdown pauses and warns if no engineer is assigned.
///   Resolve — risk calculated from assigned team at delivery time.
/// </summary>
public class ContractManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<Contract> OnContractOffered;
    public static event Action<Contract> OnContractAccepted;
    public static event Action<Contract> OnContractCompleted;
    public static event Action<Contract> OnContractFailed;
    public static event Action<Contract> OnContractUnstaffed;   // Fires when countdown would tick but no engineer is assigned.
    public static event Action<IReadOnlyList<Contract>> OnOffersUpdated;
    public static event Action<IReadOnlyList<Contract>> OnActiveContractsUpdated;

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private ContractTemplateSO[] _contractTemplates;
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private EmployeeManager _employeeManager;
    [SerializeField] private WorldEventManager _worldEventManager;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<Contract> AvailableContracts => _availableContracts;
    public IReadOnlyList<Contract> ActiveContracts => _activeContracts;

    // — Private State —————————————————————————————————————
    private readonly List<Contract> _availableContracts = new();
    private readonly List<Contract> _activeContracts = new();
    private readonly HashSet<string> _completedNodeIds = new();

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
    /// Accepts a contract offer. Commitment is locked — contract cannot be un-accepted.
    /// No engineer assignment occurs here. Player assigns engineers via EMP panel.
    /// Risk is calculated at delivery, not here.
    /// </summary>
    public bool AcceptContract(Contract contract)
    {
        if (!_availableContracts.Contains(contract))
        {
            Debug.LogWarning($"[ContractManager] AcceptContract: " +
                             $"'{contract.ContractId}' not in available pool.");
            return false;
        }

        contract.IsActive = true;
        _availableContracts.Remove(contract);
        _activeContracts.Add(contract);

        OnContractAccepted?.Invoke(contract);
        OnActiveContractsUpdated?.Invoke(_activeContracts);

        TopUpOfferPool();

        Debug.Log($"[ContractManager] Accepted: {contract.ContractId} — " +
                  $"Deadline: {contract.DeadlineWeeks} weeks. " +
                  $"Assign engineers via EMP panel.");
        return true;
    }

    /// <summary>
    /// Assigns an engineer to an active contract.
    /// Called by GameHudController when the player confirms assignment in EMP panel.
    /// </summary>
    public bool AssignEngineer(string contractId, Employee engineer)
    {
        Contract contract = FindActiveContract(contractId);
        if (contract == null)
        {
            Debug.LogWarning($"[ContractManager] AssignEngineer: contract '{contractId}' not found.");
            return false;
        }

        if (contract.AssignedEmployeeIds.Contains(engineer.EmployeeId))
        {
            Debug.LogWarning($"[ContractManager] {engineer.Name} already assigned to {contractId}.");
            return false;
        }

        if (!string.IsNullOrEmpty(engineer.Assignment))
        {
            Debug.LogWarning($"[ContractManager] {engineer.Name} is already assigned to " +
                             $"'{engineer.Assignment}'. Unassign first.");
            return false;
        }

        contract.AssignedEmployeeIds.Add(engineer.EmployeeId);
        contract.HasWarnedAboutNoEngineer = false;  // Reset stall warning.
        engineer.Assignment = contractId;

        OnActiveContractsUpdated?.Invoke(_activeContracts);

        Debug.Log($"[ContractManager] {engineer.Name} assigned to {contractId}. " +
                  $"Team size: {contract.AssignedEmployeeIds.Count}.");
        return true;
    }

    /// <summary>
    /// Removes an engineer from an active contract.
    /// Called if the player reassigns the engineer elsewhere.
    /// </summary>
    public bool UnassignEngineer(string contractId, string engineerId)
    {
        Contract contract = FindActiveContract(contractId);
        if (contract == null) return false;

        if (!contract.AssignedEmployeeIds.Remove(engineerId)) return false;

        Employee engineer = _employeeManager?.GetEmployeeById(engineerId);
        if (engineer != null && engineer.Assignment == contractId)
            engineer.Assignment = null;

        OnActiveContractsUpdated?.Invoke(_activeContracts);
        return true;
    }

    /// <summary>
    /// Returns the display name of the engineer assigned to a contract, or null if none.
    /// Used by ContractPanel for read-only assignment display.
    /// </summary>
    public string GetAssignedEngineerName(Contract contract)
    {
        if (contract.AssignedEmployeeIds.Count == 0) return null;

        Employee eng = _employeeManager?.GetEmployeeById(contract.AssignedEmployeeIds[0]);
        return eng?.Name;
    }

    /// <summary>Returns an active contract by ID, or null.</summary>
    public Contract FindActiveContract(string contractId)
    {
        foreach (Contract c in _activeContracts)
            if (c.ContractId == contractId) return c;
        return null;
    }

    /// <summary>Calculates current success chance for a contract based on its assigned team.</summary>
    public float GetCurrentSuccessChance(Contract contract)
    {
        var engineers = GetAssignedEngineers(contract);
        if (engineers.Count == 0) return AegisConstants.MIN_CONTRACT_CHANCE;

        return CalculateSuccessChance(
            engineers,
            requiredEngineerCount: Mathf.Max(1, engineers.Count),
            contractReputationTier: contract.ReputationTierRequired,
            budgetAllocated: contract.BudgetAllocated,
            contractBaseCost: contract.BaseCostGBP);
    }

    // — Save/Load (unchanged from Sub-session A) ———————————
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
            foreach (var d in data.AvailableContracts)
                _availableContracts.Add(SaveDataToContract(d));

        if (data.ActiveContracts != null)
            foreach (var d in data.ActiveContracts)
                _activeContracts.Add(SaveDataToContract(d));

        _completedNodeIds.Clear();
        SeedCompletedResearch();

        OnOffersUpdated?.Invoke(_availableContracts);
        OnActiveContractsUpdated?.Invoke(_activeContracts);

        Debug.Log($"[ContractManager] Loaded. Available: {_availableContracts.Count}, " +
                  $"Active: {_activeContracts.Count}.");
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
        bool anyStateChanged = false;

        foreach (Contract contract in _activeContracts)
        {
            if (contract.AssignedEmployeeIds.Count == 0)
            {
                // Stall: countdown does not advance without an engineer.
                if (!contract.HasWarnedAboutNoEngineer)
                {
                    contract.HasWarnedAboutNoEngineer = true;
                    OnContractUnstaffed?.Invoke(contract);
                    Debug.LogWarning($"[ContractManager] Contract {contract.ContractId} " +
                                     $"is stalled — no engineer assigned.");
                }
                continue;
            }

            // Engineer is assigned — countdown proceeds, reset stall warning.
            contract.HasWarnedAboutNoEngineer = false;
            contract.WeeksRemaining--;
            anyStateChanged = true;

            if (contract.WeeksRemaining <= 0)
                toResolve.Add(contract);
        }

        foreach (Contract contract in toResolve)
            ResolveContract(contract);

        if (anyStateChanged || toResolve.Count > 0)
            OnActiveContractsUpdated?.Invoke(_activeContracts);
    }

    private void ResolveContract(Contract contract)
    {
        _activeContracts.Remove(contract);

        // Risk calculated at delivery from the team currently assigned.
        // DD-12 (updated): commitment locked at acceptance, risk locked at delivery.
        List<Employee> engineers = GetAssignedEngineers(contract);
        contract.LockedSuccessChance = CalculateSuccessChance(
            engineers,
            requiredEngineerCount: Mathf.Max(1, engineers.Count),
            contractReputationTier: contract.ReputationTierRequired,
            budgetAllocated: contract.BudgetAllocated,
            contractBaseCost: contract.BaseCostGBP);

        bool success = Random.Range(0f, 100f) < contract.LockedSuccessChance;

        if (success)
        {
            OnContractCompleted?.Invoke(contract);
            Debug.Log($"[ContractManager] SUCCESS: {contract.ContractId} " +
                      $"(chance was {contract.LockedSuccessChance:F1}%).");
        }
        else
        {
            OnContractFailed?.Invoke(contract);
            Debug.Log($"[ContractManager] FAILED: {contract.ContractId} " +
                      $"(chance was {contract.LockedSuccessChance:F1}%).");
        }

        FreeAssignedEngineers(contract);
    }

    private List<Employee> GetAssignedEngineers(Contract contract)
    {
        var list = new List<Employee>();
        if (_employeeManager == null) return list;

        foreach (string id in contract.AssignedEmployeeIds)
        {
            Employee emp = _employeeManager.GetEmployeeById(id);
            if (emp != null) list.Add(emp);
        }
        return list;
    }

    private void FreeAssignedEngineers(Contract contract)
    {
        if (_employeeManager == null) return;
        foreach (string id in contract.AssignedEmployeeIds)
        {
            Employee emp = _employeeManager.GetEmployeeById(id);
            if (emp != null && emp.Assignment == contract.ContractId)
                emp.Assignment = null;
        }
    }

    // — Private: Events ————————————————————————————————————

    private void HandleResearchCompleted(ResearchNodeSO node)
    {
        _completedNodeIds.Add(node.NodeId);
        TopUpOfferPool();
    }

    private void HandleTierChanged(int newTier)
    {
        _playerRepTier = newTier;
        TopUpOfferPool();
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
            // Weight selection by demand multiplier so high-demand categories appear more.
            ContractTemplateSO template = SelectWeightedTemplate(eligible);
            _availableContracts.Add(GenerateFromTemplate(template));
        }

        OnOffersUpdated?.Invoke(_availableContracts);
    }

    private ContractTemplateSO SelectWeightedTemplate(List<ContractTemplateSO> eligible)
    {
        float totalWeight = 0f;
        foreach (var t in eligible)
            totalWeight += _worldEventManager?.GetDemandMultiplier(t.ContractCategory) ?? 1f;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var t in eligible)
        {
            cumulative += _worldEventManager?.GetDemandMultiplier(t.ContractCategory) ?? 1f;
            if (roll <= cumulative) return t;
        }

        return eligible[eligible.Count - 1]; // Fallback.
    }

    private List<ContractTemplateSO> GetEligibleTemplates()
    {
        var eligible = new List<ContractTemplateSO>();
        foreach (var t in _contractTemplates)
        {
            if (t == null) continue;
            if (t.MinReputationTier > _playerRepTier) continue;
            if (t.RequiredResearch != null &&
                !_completedNodeIds.Contains(t.RequiredResearch.NodeId)) continue;
            eligible.Add(t);
        }
        return eligible;
    }

    private Contract GenerateFromTemplate(ContractTemplateSO template)
    {
        float normRep = (_playerRepTier - 1) / 4f;
        float repMult = template.RewardScaleByReputation?.Evaluate(normRep) ?? 1f;

        // World event modifiers stack on top of reputation scaling.
        float demandMult = _worldEventManager?.GetDemandMultiplier(template.ContractCategory) ?? 1f;
        float rewardMult = _worldEventManager?.GetRewardMultiplier(template.ContractCategory) ?? 1f;

        return new Contract
        {
            ContractId = $"CON_{++_contractIdCounter:D4}",
            ClientRegion = "Unknown Region",
            ContractCategory = template.ContractCategory,
            ReputationTierRequired = template.MinReputationTier,
            BaseRewardGBP = template.BaseRewardGBP * repMult * rewardMult,
            BaseCostGBP = template.BaseRewardGBP * 0.3f,
            DeadlineWeeks = template.BaseDeadlineWeeks,
            WeeksRemaining = template.BaseDeadlineWeeks,
            BudgetAllocated = 0f,
            IsActive = false,
            LockedSuccessChance = 0f,
            LockedEngineerCount = 0
        };
    }

    // — Private: Risk Formula (unchanged) —————————————————

    public float CalculateSuccessChance(IReadOnlyList<Employee> engineers,
        int requiredEngineerCount, int contractReputationTier,
        float budgetAllocated, float contractBaseCost)
    {
        if (engineers == null || engineers.Count == 0)
            return AegisConstants.MIN_CONTRACT_CHANCE;

        float teamBonus = CalculateTeamBonus(engineers, requiredEngineerCount);
        float complexityPenalty = (contractReputationTier - 1) * AegisConstants.COMPLEXITY_PENALTY_PER_TIER;
        float budgetBonus = contractBaseCost <= 0f ? 0f :
            Mathf.Clamp((budgetAllocated / contractBaseCost - 1f) * AegisConstants.MAX_BUDGET_BONUS,
                        0f, AegisConstants.MAX_BUDGET_BONUS);

        float raw = AegisConstants.BASE_CONTRACT_CHANCE + teamBonus + budgetBonus - complexityPenalty;
        return Mathf.Clamp(raw, AegisConstants.MIN_CONTRACT_CHANCE, AegisConstants.MAX_CONTRACT_CHANCE);
    }

    private float CalculateTeamBonus(IReadOnlyList<Employee> engineers, int required)
    {
        float staffing = Mathf.Min(1f, (float)engineers.Count / required);
        float total = 0f;
        foreach (var eng in engineers)
            total += eng.GetModifiedStat(AegisConstants.STAT_EFFICIENCY) * 0.5f
                   + eng.GetModifiedStat(AegisConstants.STAT_INTELLIGENCE) * 0.3f
                   + eng.GetModifiedStat(AegisConstants.STAT_CREATIVITY) * 0.2f;

        return ((total / engineers.Count) * staffing - 50f) * AegisConstants.TEAM_BONUS_MULTIPLIER;
    }

    // — Save/Load helpers (unchanged from Sub-session A) ——

    private static List<ContractSaveData> ContractsToSaveData(List<Contract> contracts)
    {
        var list = new List<ContractSaveData>();
        foreach (var c in contracts)
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
        return list;
    }

    private static Contract SaveDataToContract(ContractSaveData d) => new Contract
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
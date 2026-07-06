using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Generates contract offers from ContractTemplateSO data.
/// Filters by player reputation tier and completed research.
/// M2: generation and offer pool management.
/// M3: acceptance, delivery, resolution, reputation impact.
/// </summary>
public class ContractManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<Contract> OnContractOffered;
    public static event Action<Contract> OnContractAccepted;
    public static event Action<Contract> OnContractCompleted;
    public static event Action<Contract> OnContractFailed;
    public static event Action<IReadOnlyList<Contract>> OnOffersUpdated;

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private ContractTemplateSO[] _contractTemplates;
    [SerializeField] private ResearchManager _researchManager;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<Contract> AvailableContracts => _availableContracts;
    public IReadOnlyList<Contract> ActiveContracts => _activeContracts;

    // — Private State —————————————————————————————————————
    private readonly List<Contract> _availableContracts = new();
    private readonly List<Contract> _activeContracts = new();

    // Seeded at Start from ResearchManager.NodeStates snapshot.
    // Stays current via OnResearchCompleted subscription.
    // Exception to event-only pattern — one-time read at startup only.
    private readonly HashSet<string> _completedNodeIds = new();

    // Reputation hardcoded to Phase 1 pending ReputationManager (M3).
    // Same pattern as QA-004. Do not remove this comment when wiring.
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
    }

    private void OnDisable()
    {
        TimeManager.OnWeekTick -= HandleWeekTick;
        ResearchManager.OnResearchCompleted -= HandleResearchCompleted;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Calculates contract success chance per DD-09/OQ-02.
    /// Risk locks at acceptance — this is called once when the player accepts,
    /// stored on the Contract, and never recalculated. See design decision record.
    /// </summary>
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

    // — Private Methods ————————————————————————————————————

    private void HandleWeekTick()
    {
        TickActiveContracts();
        TopUpOfferPool();
    }

    private void HandleResearchCompleted(ResearchNodeSO node)
    {
        _completedNodeIds.Add(node.NodeId);
        // New research may unlock previously filtered templates — refresh the pool.
        TopUpOfferPool();
    }

    private void SeedCompletedResearch()
    {
        if (_researchManager == null) return;

        foreach (var kvp in _researchManager.NodeStates)
        {
            if (kvp.Value == ResearchNodeState.Complete)
                _completedNodeIds.Add(kvp.Key);
        }
    }

    private void TopUpOfferPool()
    {
        if (_contractTemplates == null || _contractTemplates.Length == 0) return;

        var eligible = GetEligibleTemplates();
        if (eligible.Count == 0) return;

        while (_availableContracts.Count < AegisConstants.CONTRACT_POOL_SIZE)
        {
            var template = eligible[Random.Range(0, eligible.Count)];
            var contract = GenerateFromTemplate(template);
            _availableContracts.Add(contract);
            OnContractOffered?.Invoke(contract);
        }

        if (_availableContracts.Count > 0)
            OnOffersUpdated?.Invoke(_availableContracts);
    }

    private List<ContractTemplateSO> GetEligibleTemplates()
    {
        var eligible = new List<ContractTemplateSO>();

        foreach (var template in _contractTemplates)
        {
            if (template == null) continue;

            // Reputation gate.
            if (template.MinReputationTier > _playerRepTier) continue;

            // Research gate — null means no tech required.
            if (template.RequiredResearch != null &&
                !_completedNodeIds.Contains(template.RequiredResearch.NodeId)) continue;

            eligible.Add(template);
        }

        return eligible;
    }

    private Contract GenerateFromTemplate(ContractTemplateSO template)
    {
        // Reputation normalised to 0–1 for AnimationCurve evaluation.
        float normalisedRep = (_playerRepTier - 1) / 4f;
        float rewardMultiplier = template.RewardScaleByReputation != null
            ? template.RewardScaleByReputation.Evaluate(normalisedRep)
            : 1f;

        return new Contract
        {
            ContractId = $"CON_{++_contractIdCounter:D4}",
            ClientRegion = "Unknown Region", // M3: wire to WorldEventManager
            ContractCategory = template.ContractCategory,
            ReputationTierRequired = template.MinReputationTier,
            BaseRewardGBP = template.BaseRewardGBP * rewardMultiplier,
            BaseCostGBP = template.BaseRewardGBP * 0.3f, // 30% cost ratio — SD to review
            DeadlineWeeks = template.BaseDeadlineWeeks,
            WeeksRemaining = template.BaseDeadlineWeeks,
            BudgetAllocated = 0f,
            IsActive = false
        };
    }

    private void TickActiveContracts()
    {
        var toResolve = new List<Contract>();

        foreach (var contract in _activeContracts)
        {
            contract.WeeksRemaining--;
            if (contract.WeeksRemaining <= 0)
                toResolve.Add(contract);
        }

        // M3: resolve with reputation impact and finance credit.
        // M2: log only — resolution logic not yet implemented.
        foreach (var contract in toResolve)
        {
            _activeContracts.Remove(contract);
            Debug.Log($"[ContractManager] Contract {contract.ContractId} expired — resolution pending M3.");
        }
    }

    // — Risk Formula (unchanged from M1, kept here for co-location) ——
    private float CalculateTeamBonus(IReadOnlyList<Employee> engineers, int requiredCount)
    {
        float staffingFactor = Mathf.Min(1f, (float)engineers.Count / requiredCount);
        float total = 0f;

        foreach (var eng in engineers)
        {
            total +=
                eng.GetModifiedStat(AegisConstants.STAT_EFFICIENCY) * 0.5f +
                eng.GetModifiedStat(AegisConstants.STAT_INTELLIGENCE) * 0.3f +
                eng.GetModifiedStat(AegisConstants.STAT_CREATIVITY) * 0.2f;
        }

        return ((total / engineers.Count) * staffingFactor - 50f) * AegisConstants.TEAM_BONUS_MULTIPLIER;
    }

    private float CalculateComplexityPenalty(int tier) =>
        (tier - 1) * AegisConstants.COMPLEXITY_PENALTY_PER_TIER;

    private float CalculateBudgetBonus(float allocated, float baseCost)
    {
        if (baseCost <= 0f) return 0f;
        return Mathf.Clamp((allocated / baseCost - 1f) * AegisConstants.MAX_BUDGET_BONUS,
                            0f, AegisConstants.MAX_BUDGET_BONUS);
    }
}
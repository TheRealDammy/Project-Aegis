using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages available contracts, active contracts, and contract resolution.
/// M1: CalculateSuccessChance fully implemented per OQ-02.
/// M2: Contract generation from ContractTemplateSO, delivery, and failure resolution.
/// </summary>
public class ContractManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<Contract> OnContractOffered;
    public static event Action<Contract> OnContractAccepted;
    public static event Action<Contract> OnContractCompleted;
    public static event Action<Contract> OnContractFailed;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<Contract> AvailableContracts => _availableContracts;
    public IReadOnlyList<Contract> ActiveContracts => _activeContracts;

    // — Private Fields ————————————————————————————————————
    private readonly List<Contract> _availableContracts = new List<Contract>();
    private readonly List<Contract> _activeContracts = new List<Contract>();

    // — Unity Lifecycle ————————————————————————————————————
    private void OnEnable()
    {
        TimeManager.OnWeekTick += HandleWeekTick;
    }

    private void OnDisable()
    {
        TimeManager.OnWeekTick -= HandleWeekTick;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Calculates contract success chance per OQ-02.
    /// assignedEngineers must be Employee objects — GetModifiedStat() is called
    /// internally, so trait modifiers are always applied correctly.
    /// Returns a percentage clamped to MIN_CONTRACT_CHANCE–MAX_CONTRACT_CHANCE.
    /// </summary>
    public float CalculateSuccessChance(
        IReadOnlyList<Employee> assignedEngineers,
        int requiredEngineerCount,
        int contractReputationTier,
        float budgetAllocated,
        float contractBaseCost)
    {
        // Zero engineers means guaranteed near-failure — return floor immediately
        // to avoid division-by-zero and give the player a clear signal.
        if (assignedEngineers == null || assignedEngineers.Count == 0)
            return AegisConstants.MIN_CONTRACT_CHANCE;

        float teamBonus = CalculateTeamBonus(assignedEngineers, requiredEngineerCount);
        float complexityPenalty = CalculateComplexityPenalty(contractReputationTier);
        float budgetBonus = CalculateBudgetBonus(budgetAllocated, contractBaseCost);

        float raw = AegisConstants.BASE_CONTRACT_CHANCE
                  + teamBonus
                  + budgetBonus
                  - complexityPenalty;

        return Mathf.Clamp(raw, AegisConstants.MIN_CONTRACT_CHANCE, AegisConstants.MAX_CONTRACT_CHANCE);
    }

    // — Private Methods ————————————————————————————————————

    private void HandleWeekTick()
    {
        // M2: advance active contract progress, check deadlines, trigger resolution.
        TickActiveContracts();
    }

    private void TickActiveContracts()
    {
        // Placeholder — M2 wires delivery and failure logic here.
        foreach (Contract contract in _activeContracts)
            contract.WeeksRemaining--;
    }

    /// <summary>
    /// TeamBonus = (AverageTeamEffectiveness - 50) × TEAM_BONUS_MULTIPLIER.
    /// Range: −30 to +30. Staffing factor scales down under-staffed teams.
    /// </summary>
    private float CalculateTeamBonus(IReadOnlyList<Employee> engineers, int requiredCount)
    {
        // Under-staffing penalises proportionally. A team at 50% headcount
        // has its effectiveness halved before the bonus is calculated.
        float staffingFactor = Mathf.Min(1f, (float)engineers.Count / requiredCount);

        float totalEffectiveness = 0f;
        foreach (Employee eng in engineers)
        {
            // OQ-02 stat weights for Engineers: Efficiency 50%, Intelligence 30%, Creativity 20%.
            // GetModifiedStat applies trait modifiers — satisfies the "trait-modified values" requirement.
            float effectiveness =
                eng.GetModifiedStat(AegisConstants.STAT_EFFICIENCY) * 0.5f +
                eng.GetModifiedStat(AegisConstants.STAT_INTELLIGENCE) * 0.3f +
                eng.GetModifiedStat(AegisConstants.STAT_CREATIVITY) * 0.2f;

            totalEffectiveness += effectiveness;
        }

        float averageEffectiveness = (totalEffectiveness / engineers.Count) * staffingFactor;
        return (averageEffectiveness - 50f) * AegisConstants.TEAM_BONUS_MULTIPLIER;
    }

    /// <summary>
    /// ComplexityPenalty = (Tier − 1) × COMPLEXITY_PENALTY_PER_TIER.
    /// Range: 0 (Tier 1) to 32 (Tier 5). Higher-tier contracts punish weak teams.
    /// </summary>
    private float CalculateComplexityPenalty(int reputationTier)
    {
        return (reputationTier - 1) * AegisConstants.COMPLEXITY_PENALTY_PER_TIER;
    }

    /// <summary>
    /// BudgetBonus = Clamp((BudgetAllocated / BaseCost − 1.0) × MAX_BUDGET_BONUS, 0, MAX).
    /// Range: 0–20. Spending exactly 1× base yields 0. Spending 2× base yields full 20.
    /// </summary>
    private float CalculateBudgetBonus(float budgetAllocated, float baseCost)
    {
        if (baseCost <= 0f)
        {
            Debug.LogWarning("[ContractManager] CalculateBudgetBonus: baseCost is zero or negative.");
            return 0f;
        }

        float ratio = (budgetAllocated / baseCost) - 1f;
        return Mathf.Clamp(ratio * AegisConstants.MAX_BUDGET_BONUS, 0f, AegisConstants.MAX_BUDGET_BONUS);
    }
}
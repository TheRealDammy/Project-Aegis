using System;
using UnityEngine;

/// <summary>
/// Checks win conditions each week. Records as DD-16.
/// Victory fires once and does not end the game — player continues in sandbox mode.
/// Sandbox mode flag suppresses all checks silently.
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<VictoryType, string> OnVictoryAchieved;

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private FinanceManager _financeManager;
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private ReputationManager _reputationManager;
    [SerializeField] private MarketManager _marketManager;

    /// <summary>
    /// When true, all win condition checks are silently suppressed.
    /// Set in Inspector for Sandbox mode. Wired to New Game dialog in post-launch.
    /// </summary>
    [SerializeField] private bool _sandboxMode = false;

    // — Private State —————————————————————————————————————
    /// <summary>Prevents the same victory from firing multiple times per session.</summary>
    private bool _victoryAchieved = false;

    // — Unity Lifecycle ————————————————————————————————————
    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Private ————————————————————————————————————————————

    private void HandleWeekTick()
    {
        if (_sandboxMode || _victoryAchieved) return;

        if (CheckFinancialVictory()) DeclareVictory(VictoryType.Financial);
        else if (CheckTechnologyVictory()) DeclareVictory(VictoryType.Technology);
        else if (CheckMarketVictory()) DeclareVictory(VictoryType.Market);
    }

    private bool CheckFinancialVictory()
    {
        if (_financeManager == null || _researchManager == null || _reputationManager == null)
            return false;

        int completedNodes = CountCompletedResearchNodes();
        float valuation =
            _financeManager.CashBalance
            + (_financeManager.WeeklyRevenueAverage * AegisConstants.WIN_FINANCIAL_REVENUE_MULTIPLIER)
            + (completedNodes * AegisConstants.WIN_FINANCIAL_RESEARCH_VALUE)
            + (_reputationManager.ReputationScore * AegisConstants.WIN_FINANCIAL_REPUTATION_VALUE);

        return valuation >= AegisConstants.WIN_FINANCIAL_VALUATION_TARGET;
    }

    private bool CheckTechnologyVictory()
    {
        if (_researchManager == null) return false;
        return CountCompletedResearchNodes() >= AegisConstants.WIN_TECHNOLOGY_NODE_COUNT;
    }

    private bool CheckMarketVictory()
    {
        if (_marketManager == null) return false;
        return _marketManager.GetAveragePlayerShare() >= AegisConstants.WIN_MARKET_SHARE_THRESHOLD;
    }

    private int CountCompletedResearchNodes()
    {
        int count = 0;
        foreach (ResearchNodeState state in _researchManager.NodeStates.Values)
            if (state == ResearchNodeState.Complete) count++;
        return count;
    }

    private void DeclareVictory(VictoryType type)
    {
        _victoryAchieved = true;

        string description = type switch
        {
            VictoryType.Financial => $"Company valuation reached £{AegisConstants.WIN_FINANCIAL_VALUATION_TARGET / 1_000_000}M.",
            VictoryType.Technology => "All 17 research nodes completed.",
            VictoryType.Market => $"Average market share exceeded {AegisConstants.WIN_MARKET_SHARE_THRESHOLD * 100f:F0}%.",
            _ => string.Empty
        };

        OnVictoryAchieved?.Invoke(type, description);
        Debug.Log($"[WinConditionManager] VICTORY — {type}: {description}");
    }
}
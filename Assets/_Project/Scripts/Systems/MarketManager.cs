using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks percentage market share per research branch for the player and rivals.
/// M5: Calculates and stores share. Market panel display is a future milestone.
/// Share = entity_branch_score / total_branch_scores_across_all_entities.
/// </summary>
public class MarketManager : MonoBehaviour
{
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private RivalManager _rivalManager;

    // PlayerShare[branch] = 0.0–1.0 (percentage 0–100%)
    public Dictionary<ResearchBranch, float> PlayerShare { get; private set; } = new();

    private void OnEnable()
    {
        RivalManager.OnRivalProgressUpdated += RecalculateShares;
        ResearchManager.OnResearchCompleted += _ => RecalculateShares();
    }

    private void OnDisable()
    {
        RivalManager.OnRivalProgressUpdated -= RecalculateShares;
        ResearchManager.OnResearchCompleted -= _ => RecalculateShares();
    }

    private void Start() => RecalculateShares();

    /// <summary>
    /// Returns the average player market share across all four branches (0.0–1.0).
    /// Used by WinConditionManager for the Market Victory condition.
    /// 0.60 = 60% average share = market victory.
    /// </summary>
    public float GetAveragePlayerShare()
    {
        if (PlayerShare.Count == 0) return 0f;

        float sum = 0f;
        foreach (float share in PlayerShare.Values)
            sum += share;

        return sum / PlayerShare.Count;
    }

    // — Save / Load ————————————————————————————————————————

    public void PopulateSaveData(GameSaveData data)
    {
        data.MarketShare = new Dictionary<string, float>();
        foreach (var kvp in PlayerShare)
            data.MarketShare[kvp.Key.ToString()] = kvp.Value;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        // Market share is fully derived from research + rival progress.
        // Recalculate rather than load — avoids stale data issues.
        RecalculateShares();
    }

    // — Private ————————————————————————————————————————————

    private void RecalculateShares()
    {
        if (_researchManager == null || _rivalManager == null) return;

        foreach (ResearchBranch branch in System.Enum.GetValues(typeof(ResearchBranch)))
        {
            float playerScore = GetPlayerBranchScore(branch);
            float totalScore = playerScore;

            foreach (RivalProgressData rival in _rivalManager.Rivals)
                totalScore += GetRivalBranchScore(rival, branch);

            PlayerShare[branch] = totalScore > 0f ? playerScore / totalScore : 0f;
        }
    }

    private float GetPlayerBranchScore(ResearchBranch branch)
    {
        // Each complete research node in the branch contributes 20 points (max 5 nodes → 100).
        int complete = 0;
        foreach (var kvp in _researchManager.NodeStates)
        {
            if (kvp.Value != ResearchNodeState.Complete) continue;
            if (_researchManager.NodeSOLookup.TryGetValue(kvp.Key, out ResearchNodeSO so)
                && so.Branch == branch)
                complete++;
        }
        return complete * 20f;
    }

    private static float GetRivalBranchScore(RivalProgressData rival, ResearchBranch branch)
    {
        return rival.BranchProgress.TryGetValue(branch, out float val) ? val : 0f;
    }
}
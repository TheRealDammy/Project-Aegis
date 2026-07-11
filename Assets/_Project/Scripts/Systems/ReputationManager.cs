using System;
using UnityEngine;

/// <summary>
/// Tracks reputation score (0–100) and five-tier progression.
/// Adjusts score on contract outcomes. Fires OnTierChanged when tier advances or drops.
/// Subscribes to ContractManager events — never called directly by ContractManager.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    /// <summary>Fires on every score change, with new score value.</summary>
    public static event Action<float> OnReputationChanged;

    /// <summary>
    /// Fires when the tier number changes. Subscribers (EmployeeManager, ContractManager)
    /// use this to update phase-gated behaviour. Closes QA-004.
    /// </summary>
    public static event Action<int> OnTierChanged;

    // — Public Properties ——————————————————————————————————
    public float ReputationScore { get; private set; }
    public int CurrentTier { get; private set; }

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        ReputationScore = AegisConstants.STARTING_REPUTATION;
        CurrentTier = CalculateTier(ReputationScore);
        OnReputationChanged?.Invoke(ReputationScore);
        Debug.Log($"[ReputationManager] Starting reputation: {ReputationScore} — Tier {CurrentTier}.");
    }

    private void OnEnable()
    {
        ContractManager.OnContractCompleted += HandleContractSuccess;
        ContractManager.OnContractFailed += HandleContractFailed;
    }

    private void OnDisable()
    {
        ContractManager.OnContractCompleted -= HandleContractSuccess;
        ContractManager.OnContractFailed -= HandleContractFailed;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>Returns the display name for a given tier number (1–5).</summary>
    public static string GetTierName(int tier) => tier switch
    {
        1 => "STARTUP",
        2 => "TRUSTED",
        3 => "MAJOR SUPPLIER",
        4 => "INDUSTRY LEADER",
        5 => "GLOBAL GIANT",
        _ => "UNKNOWN"
    };

    // — Private Methods ————————————————————————————————————

    private void HandleContractSuccess(Contract contract)
    {
        float delta = contract.ReputationTierRequired * AegisConstants.REPUTATION_SUCCESS_PER_TIER;
        ApplyScoreDelta(delta);
        Debug.Log($"[ReputationManager] Contract success: +{delta} reputation. " +
                  $"Score: {ReputationScore:F1} (Tier {CurrentTier}).");
    }

    private void HandleContractFailed(Contract contract)
    {
        float delta = contract.ReputationTierRequired * AegisConstants.REPUTATION_FAILURE_PER_TIER;
        ApplyScoreDelta(-delta);
        Debug.Log($"[ReputationManager] Contract failure: -{delta} reputation. " +
                  $"Score: {ReputationScore:F1} (Tier {CurrentTier}).");
    }

    private void ApplyScoreDelta(float delta)
    {
        float previousScore = ReputationScore;
        ReputationScore = Mathf.Clamp(ReputationScore + delta, 0f, 100f);

        OnReputationChanged?.Invoke(ReputationScore);

        int newTier = CalculateTier(ReputationScore);
        if (newTier != CurrentTier)
        {
            int previousTier = CurrentTier;
            CurrentTier = newTier;
            OnTierChanged?.Invoke(CurrentTier);
            Debug.Log($"[ReputationManager] Tier changed: {previousTier} → {CurrentTier} " +
                      $"({GetTierName(CurrentTier)}).");
        }
    }

    private static int CalculateTier(float score)
    {
        if (score >= AegisConstants.REPUTATION_TIER_5_THRESHOLD) return 5;
        if (score >= AegisConstants.REPUTATION_TIER_4_THRESHOLD) return 4;
        if (score >= AegisConstants.REPUTATION_TIER_3_THRESHOLD) return 3;
        if (score >= AegisConstants.REPUTATION_TIER_2_THRESHOLD) return 2;
        return 1;
    }
}
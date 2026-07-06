using UnityEngine;

/// <summary>
/// Template defining a contract category. ContractManager generates
/// runtime Contract instances from these at play time.
/// AnimationCurve maps normalised reputation (0=Tier1, 1=Tier5) to a reward multiplier.
/// </summary>
[CreateAssetMenu(menuName = "Aegis/Contract Template", fileName = "NewContractTemplate")]
public class ContractTemplateSO : ScriptableObject
{
    /// <summary>
    /// Category string (e.g. "Drone.Recon", "Cyber.Security").
    /// Naming convention pending OQ-01 product set resolution.
    /// </summary>
    public string ContractCategory;

    public float BaseRewardGBP;
    public int BaseDeadlineWeeks;

    /// <summary>Player reputation tier must be >= this to receive this offer.</summary>
    public int MinReputationTier;

    /// <summary>
    /// Null = no tech requirement (available from Phase 1).
    /// Set to a ResearchNodeSO = that node must be Complete before this offer appears.
    /// </summary>
    public ResearchNodeSO RequiredResearch;

    /// <summary>
    /// X axis: normalised reputation tier (Tier1=0.0, Tier5=1.0).
    /// Y axis: reward multiplier applied to BaseRewardGBP.
    /// Default curve: flat at 1.0. Set an upward slope so high-rep players
    /// earn more from the same category.
    /// </summary>
    public AnimationCurve RewardScaleByReputation;
}
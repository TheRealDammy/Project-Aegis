using System;
using UnityEngine;

/// <summary>
/// Defines a world event type. Authored as ScriptableObject assets.
/// Runtime instances use ActiveWorldEvent to track remaining duration.
/// </summary>
[CreateAssetMenu(menuName = "Aegis/World Event", fileName = "NewWorldEvent")]
public class WorldEventSO : ScriptableObject
{
    public string EventId;
    public string EventName;
    [TextArea(2, 4)]
    public string Description;

    /// <summary>How many weeks this event lasts when triggered.</summary>
    public int DurationWeeks;

    /// <summary>
    /// Per-category multipliers while this event is active.
    /// ContractManager aggregates these across all concurrent active events.
    /// </summary>
    public ContractCategoryModifier[] MarketModifiers;

    /// <summary>
    /// Rival branch progress rate multipliers while this event is active.
    /// Keyed by rival name to match RivalManager.Rivals[n].Name.
    /// </summary>
    public RivalProgressModifier[] RivalModifiers;
}

/// <summary>Modifies contract offer frequency and reward for a specific category.</summary>
[Serializable]
public class ContractCategoryModifier
{
    /// <summary>Must match ContractTemplateSO.ContractCategory exactly.</summary>
    public string ContractCategory;

    /// <summary>
    /// Multiplier on how often this category appears in the offer pool.
    /// 1.0 = no change. 1.5 = 50% more frequent. 0.5 = half as frequent.
    /// </summary>
    public float DemandMultiplier = 1f;

    /// <summary>Multiplier on BaseRewardGBP for this category while event is active.</summary>
    public float RewardMultiplier = 1f;
}

/// <summary>Modifies a rival's weekly progress rate while an event is active.</summary>
[Serializable]
public class RivalProgressModifier
{
    /// <summary>Must match RivalProgressData.Name.</summary>
    public string RivalName;

    /// <summary>Flat addition to weekly progress rate across all branches.</summary>
    public float ProgressRateBonus;
}
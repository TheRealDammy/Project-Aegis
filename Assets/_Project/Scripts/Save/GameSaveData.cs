using System;
using System.Collections.Generic;

/// <summary>
/// Root save data object. Serialized to JSON by SaveManager.
/// Rules:
///   — No Unity types (Vector3, Color, AnimationCurve).
///   — No ScriptableObject references — string IDs only.
///   — No MonoBehaviour references.
///   — All nested types must be [Serializable].
/// Fields for unimplemented systems (WorldEvents, Rivals, Market) are present
/// for forward compatibility — they will be populated when those managers exist.
/// </summary>
[Serializable]
public class GameSaveData
{
    // — Metadata ——————————————————————————————————————————————
    /// <summary>
    /// Increment this when the save format changes in a breaking way.
    /// SaveManager checks this on load and can reject incompatible saves.
    /// </summary>
    public int SaveVersion = AegisConstants.SAVE_VERSION_CURRENT;   // Now 3
    public string SaveTimestamp;   // ISO 8601, set on save.

    // — Time ———————————————————————————————————————————————————
    public int CurrentWeek;

    // — Finance ————————————————————————————————————————————————
    public float CashBalance;

    // — Reputation ————————————————————————————————————————————
    // Tier is derived from score on load — not saved separately.
    public float ReputationScore;

    // — Employees —————————————————————————————————————————————
    public int EmployeeIdCounter;  // Must restore to prevent ID collisions.
    public List<EmployeeSaveData> Employees;
    public List<HiringCandidateSaveData> HiringPool;

    // — Research ——————————————————————————————————————————————
    // Locked/Available states are derived from these two lists on load.
    // Do not save every node's state — derive it.
    public List<string> CompletedResearchNodeIds;
    public List<ActiveResearchSaveData> ActiveResearch;

    // — Contracts ——————————————————————————————————————————————
    public int ContractIdCounter;
    public List<ContractSaveData> AvailableContracts;
    public List<ContractSaveData> ActiveContracts;

    // — Forward compatibility stubs ————————————————————————————
    public List<ActiveWorldEventSaveData> ActiveWorldEvents;   // Populated from M5.
    public int WeeksUntilNextWorldEvent;
    public Dictionary<string, float[]> RivalProgress;
    public Dictionary<string, float> MarketShare;

    // — Tutorial (added SaveVersion 3) ————————————————————————
    /// <summary>True if the player has completed or skipped the tutorial.</summary>
    public bool TutorialComplete;

    // — Finance (added SaveVersion 3) —————————————————————————
    public float FinanceCumulativeRevenue;
    public int FinanceWeekCount;
}
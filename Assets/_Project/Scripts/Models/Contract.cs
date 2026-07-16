using System;
using System.Collections.Generic;

/// <summary>
/// Runtime state for a single contract instance — either available or active.
/// ContractTemplateSO (M2) defines the template; this holds the live state.
/// </summary>
[Serializable]
public class Contract
{
    public string ContractId;
    public string ClientRegion;
    public string ContractCategory;
    public int ReputationTierRequired;  // 1–5. Drives ComplexityPenalty in OQ-02.
    public float BaseRewardGBP;
    public float BaseCostGBP;
    public int DeadlineWeeks;
    public int WeeksRemaining;
    public float BudgetAllocated;         // Player-set over-spend above BaseCostGBP.
    public bool IsActive;

    /// <summary>
    /// EmployeeIds of engineers assigned at acceptance. Looked up via GetEmployeeById().
    /// </summary>
    public List<string> AssignedEmployeeIds = new List<string>();   // Was: AssignedEmployeeNames

    /// <summary>
    /// Success chance calculated once at acceptance per DD-12.
    /// Never recalculated after this point — stores the team composition
    /// and budget allocation at the moment the player committed.
    /// </summary>
    public float LockedSuccessChance;

    /// <summary>
    /// Number of engineers on the team at acceptance.
    /// Stored for display purposes — matches AssignedEmployeeNames.Count at lock time.
    /// </summary>
    public int LockedEngineerCount;

    /// <summary>
    /// True after the first tick where this contract had no engineers assigned.
    /// Prevents spamming the notification every tick.
    /// Not saved — resets to false on load, which is correct (re-warn after reload).
    /// </summary>
    [NonSerialized]
    public bool HasWarnedAboutNoEngineer;
}
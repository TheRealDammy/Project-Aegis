using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of a runtime Contract instance.
/// No SO references — ContractCategory is a plain string.
/// LockedSuccessChance is stored as-is: risk never recalculates post-acceptance (DD-12).
/// </summary>
[Serializable]
public class ContractSaveData
{
    public string ContractId;
    public string ClientRegion;
    public string ContractCategory;
    public int ReputationTierRequired;
    public float BaseRewardGBP;
    public float BaseCostGBP;
    public int DeadlineWeeks;
    public int WeeksRemaining;
    public float BudgetAllocated;
    public bool IsActive;
    public List<string> AssignedEmployeeIds;
    public float LockedSuccessChance;
    public int LockedEngineerCount;
}
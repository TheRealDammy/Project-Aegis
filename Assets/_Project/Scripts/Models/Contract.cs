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
    public List<string> AssignedEmployeeNames = new List<string>();
}
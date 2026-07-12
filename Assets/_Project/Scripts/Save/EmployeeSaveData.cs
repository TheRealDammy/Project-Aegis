using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of a single Employee's runtime state.
/// SO references (TraitSO) are stored as TraitId strings — looked up on load.
/// Unity types (Color, Vector) are banned from this class per coding standards.
/// </summary>
[Serializable]
public class EmployeeSaveData
{
    public string EmployeeId;
    public string Name;
    public string Role;          // Stored as string — enum order may change.
    public Dictionary<string, float> Stats;
    public List<string> TraitIds;      // TraitSO.TraitId, looked up on load.
    public float WeeklySalary;
    public string Assignment;    // null/empty = unassigned.
    public float Happiness;
}

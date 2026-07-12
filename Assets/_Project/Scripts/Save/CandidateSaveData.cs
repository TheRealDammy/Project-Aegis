using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of a hiring pool candidate.
/// Mirrors HiringCandidate — preserves expiry countdown across save/load.
/// </summary>
[Serializable]
public class HiringCandidateSaveData
{
    public string EmployeeId;
    public string Name;
    public string Role;
    public Dictionary<string, float> Stats;
    public List<string> TraitIds;
    public float WeeklySalary;
    public int WeeksAvailable;
}
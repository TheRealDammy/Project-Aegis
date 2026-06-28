using System;
using System.Collections.Generic;

/// <summary>
/// A candidate visible in the weekly hiring pool. Not yet on the roster.
/// Expires after WeeksAvailable ticks and is replaced by a fresh candidate.
/// </summary>
[Serializable]
public class HiringCandidate
{
    public string Name;
    public EmployeeRole Role;
    public Dictionary<string, float> Stats = new Dictionary<string, float>();
    public List<TraitSO> Traits = new List<TraitSO>();
    public float WeeklySalary;
    public int WeeksAvailable; // Decremented by EmployeeManager on OnWeekTick.

    /// <summary>
    /// Promotes this candidate to a full Employee. Call on hire confirmation.
    /// Does not remove the candidate from the pool — EmployeeManager handles that.
    /// </summary>
    public Employee ToEmployee()
    {
        return new Employee
        {
            Name = Name,
            Role = Role,
            Stats = new Dictionary<string, float>(Stats),
            Traits = new List<TraitSO>(Traits),
            WeeklySalary = WeeklySalary,
            Assignment = null,
            Happiness = 75f  // New hires start with reasonable happiness; traits adjust from here.
        };
    }
}
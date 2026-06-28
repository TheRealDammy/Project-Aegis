using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime model for an active company roster member.
/// Not a MonoBehaviour. Not a ScriptableObject.
/// Save data uses EmployeeSaveData (converts TraitSO refs to TraitId strings).
/// </summary>
[Serializable]
public class Employee
{
    // — Identity ——————————————————————————————————————————
    public string Name;
    public EmployeeRole Role;

    // — Stats ——————————————————————————————————————————————
    // Keyed by AegisConstants.STAT_* constants. These are BASE values.
    // Always call GetModifiedStat() in simulation logic — never read Stats[] directly.
    public Dictionary<string, float> Stats = new Dictionary<string, float>();

    // — Traits —————————————————————————————————————————————
    // SO refs are correct at runtime. Strip to TraitId strings before saving.
    public List<TraitSO> Traits = new List<TraitSO>();

    // — Employment ————————————————————————————————————————
    public float WeeklySalary;
    public string Assignment;   // null or empty = unassigned; otherwise holds a project/research ID
    public float Happiness;    // 0–100

    // — Methods ——————————————————————————————————————————

    /// <summary>
    /// Returns the named stat with all trait modifiers applied.
    /// OQ-02 requires trait-modified values — always use this in risk calculations.
    /// </summary>
    public float GetModifiedStat(string statName)
    {
        if (!Stats.TryGetValue(statName, out float baseValue))
        {
            Debug.LogWarning($"[Employee:{Name}] Stat '{statName}' not found. " +
                             "Check that the role's stats were generated correctly.");
            return 0f;
        }

        float modified = baseValue;
        foreach (TraitSO trait in Traits)
        {
            foreach (StatModifier mod in trait.StatModifiers)
            {
                if (mod.StatName == statName)
                    modified += mod.Value;
            }
        }

        // Clamp prevents traits from pushing stats to nonsensical values.
        return Mathf.Clamp(modified, 0f, 100f);
    }
}
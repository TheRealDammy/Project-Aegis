using System;

/// <summary>
/// Flat additive modifier applied to a named stat.
/// Used by TraitSO to describe how a trait changes an employee's stats at runtime.
/// </summary>
[Serializable]
public class StatModifier
{
    // Must match an AegisConstants.STAT_* key. Mismatch is silent at runtime —
    // GetModifiedStat() will simply skip modifiers with unrecognised stat names.
    public string StatName;

    // Additive. Positive = boost, negative = penalty. Applied after base stat generation.
    public float Value;
}
using UnityEngine;

/// <summary>Employee trait definition. Read-only at runtime — never mutate fields during play.</summary>
[CreateAssetMenu(menuName = "Aegis/Employee Trait", fileName = "NewTrait")]
public class TraitSO : ScriptableObject
{
    /// <summary>Unique identifier. Set manually. Used for save/load lookups.</summary>
    public string TraitId;

    public string DisplayName;
    public string Description;

    /// <summary>Flat additive modifiers applied to specific stats.</summary>
    public StatModifier[] StatModifiers;

    /// <summary>Additive happiness modifier. Positive = happier baseline.</summary>
    public float HappinessModifier;

    /// <summary>Multiplicative salary modifier. 1.0 = no change. Applied to base salary.</summary>
    public float SalaryMultiplier = 1f;
}
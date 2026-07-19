using UnityEngine;

/// <summary>
/// Defines a single product in the Aegis catalogue.
/// Authored as ScriptableObject assets — one per MVP product.
/// ProductId strings are referenced by ResearchNodeSO.UnlocksProductIds.
/// </summary>
[CreateAssetMenu(menuName = "Aegis/Product", fileName = "NewProduct")]
public class ProductSO : ScriptableObject
{
    /// <summary>Unique ID. Must match ResearchNodeSO.UnlocksProductIds[] entries exactly.</summary>
    public string ProductId;

    public string DisplayName;
    public string Description;

    /// <summary>The research node that must be Complete before this product is available.</summary>
    public ResearchNodeSO RequiredResearch;

    /// <summary>
    /// Contract category strings this product satisfies.
    /// Must match ContractTemplateSO.ContractCategory values exactly.
    /// </summary>
    public string[] SatisfiesContractCategories;

    public int DevelopmentCostGBP;
    public int DevelopmentWeeks;

    /// <summary>Reputation tier of the product — 1 (basic) to 3 (advanced).</summary>
    public int ProductTier;

    public EmployeeRole[] RequiredRoles;
}
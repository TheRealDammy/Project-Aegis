using UnityEngine;

/// <summary>
/// Defines a single node in the research tree. Read-only at runtime.
/// Author one asset per node in Assets/_Project/Data/Research/.
/// NodeId must be unique — it is the key for all state dictionaries and save data.
/// </summary>
[CreateAssetMenu(menuName = "Aegis/Research Node", fileName = "NewResearchNode")]
public class ResearchNodeSO : ScriptableObject
{
    /// <summary>Unique identifier. Set manually. Never change after save data exists.</summary>
    public string NodeId;

    public string DisplayName;

    public ResearchBranch Branch;

    /// <summary>
    /// All prerequisites must be Complete before this node becomes Available.
    /// Leave empty for root nodes (Basic Drone, Basic Analytics, etc.)
    /// </summary>
    public ResearchNodeSO[] Prerequisites;

    /// <summary>
    /// Research cost in researcher-weeks. One assigned Researcher contributes
    /// their ResearchSpeed stat as progress per tick.
    /// </summary>
    public int BaseResearchCost;

    /// <summary>
    /// ProductIds unlocked when this node completes.
    /// Matched against ProductSO.ProductId at M3 when ProductSO is implemented.
    /// </summary>
    public string[] UnlocksProductIds;
}
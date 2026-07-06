using System;

/// <summary>
/// Runtime state of a node currently being researched.
/// ResearchManager maintains one instance per InProgress node.
/// Serialized to save data by ResearchManager — no SO references, string IDs only.
/// </summary>
[Serializable]
public class ActiveResearchProject
{
    /// <summary>Matches ResearchNodeSO.NodeId — looked up on load.</summary>
    public string NodeId;

    /// <summary>
    /// Assigned researcher's name — matched back to Employee.Name on load.
    /// Upgrade to a stable employee ID when save/load (M4) is implemented.
    /// </summary>
    public string AssignedResearcherName;

    /// <summary>Researcher-weeks invested so far. Compared to BaseResearchCost.</summary>
    public float Progress;
}
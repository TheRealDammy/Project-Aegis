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
    /// Assigned researcher's EmployeeId. Looked up via EmployeeManager.GetEmployeeById().
    /// Stable across sessions — survives save/load correctly.
    /// </summary>
    public string AssignedResearcherId;

    /// <summary>Researcher-weeks invested so far. Compared to BaseResearchCost.</summary>
    public float Progress;
}
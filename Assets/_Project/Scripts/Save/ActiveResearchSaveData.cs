using System;

/// <summary>
/// Serializable snapshot of an in-progress research project.
/// NodeId and AssignedResearcherId are stable IDs that survive save/load.
/// </summary>
[Serializable]
public class ActiveResearchSaveData
{
    public string NodeId;
    public string AssignedResearcherId;
    public float Progress;
}
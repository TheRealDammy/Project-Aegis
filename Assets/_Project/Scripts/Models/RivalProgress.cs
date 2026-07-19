using System;
using System.Collections.Generic;

/// <summary>
/// Runtime progress state for a single rival corporation.
/// Progress scores are 0–100 per branch. Market share is derived from these scores.
/// </summary>
[Serializable]
public class RivalProgressData
{
    public string Name;

    /// <summary>Branch where this rival advances at RIVAL_PROGRESS_SPECIALIZATION rate.</summary>
    public ResearchBranch Specialization;

    /// <summary>
    /// True for Titan Defense who has no single specialization.
    /// Uses RIVAL_TITAN_PROGRESS_ALL rate for all branches instead.
    /// </summary>
    public bool IsGeneralist;

    /// <summary>Progress 0–100 per branch. Serialized to save data as float[].</summary>
    public Dictionary<ResearchBranch, float> BranchProgress = new();

    /// <summary>Starting progress values set at game start (reflects pre-game history).</summary>
    public Dictionary<ResearchBranch, float> StartingProgress = new();
}
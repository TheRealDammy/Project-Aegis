/// <summary>
/// Lifecycle state of a research node.
/// Locked → Available (prerequisites met) → InProgress (researcher assigned) → Complete.
/// </summary>
public enum ResearchNodeState
{
    Locked,
    Available,
    InProgress,
    Complete
}
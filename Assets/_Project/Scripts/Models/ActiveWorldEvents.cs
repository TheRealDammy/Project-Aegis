using System;

/// <summary>
/// Runtime state of a world event currently in progress.
/// WeeksRemaining decrements on OnWeekTick. On zero, event expires.
/// </summary>
[Serializable]
public class ActiveWorldEvent
{
    public WorldEventSO EventSO;
    public int WeeksRemaining;
}
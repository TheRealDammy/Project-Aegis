using System;

/// <summary>
/// Serializable snapshot of an active world event.
/// EventId is the WorldEventSO.EventId — looked up on load.
/// </summary>
[Serializable]
public class ActiveWorldEventSaveData
{
    public string EventId;
    public int WeeksRemaining;
}
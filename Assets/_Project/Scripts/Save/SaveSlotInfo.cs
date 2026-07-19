using System;

/// <summary>
/// Metadata about a save slot, read without fully loading the game state.
/// Used by SettingsPanel to display slot information and compatibility status.
/// </summary>
public class SaveSlotInfo
{
    public int Slot;
    public bool IsAutosave;

    // — File state ————————————————————————————————————————————
    public bool Exists;
    public bool IsCorrupted;

    // — Compatibility —————————————————————————————————————————
    /// <summary>False if SaveVersion does not match AegisConstants.SAVE_VERSION_CURRENT.</summary>
    public bool IsCompatible;
    public int SaveVersion;

    // — Payload preview ———————————————————————————————————————
    /// <summary>ISO 8601 timestamp written at save time.</summary>
    public string Timestamp;
    public int CurrentWeek;

    // — Display helpers ————————————————————————————————————————
    public string SlotLabel => IsAutosave ? "AUTOSAVE" : $"SLOT {Slot}";

    public string StatusSummary
    {
        get
        {
            if (!Exists) return "Empty";
            if (IsCorrupted) return "Corrupted";
            if (!IsCompatible) return $"Incompatible (v{SaveVersion})";
            return $"Week {CurrentWeek}";
        }
    }

    public string FormattedTimestamp
    {
        get
        {
            if (string.IsNullOrEmpty(Timestamp)) return "—";
            if (DateTime.TryParse(Timestamp, out DateTime dt))
                return dt.ToLocalTime().ToString("d MMM yyyy  HH:mm");
            return Timestamp;
        }
    }
}
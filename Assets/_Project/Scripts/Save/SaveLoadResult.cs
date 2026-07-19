/// <summary>
/// Result of a save load attempt. Replaces the bare bool previously returned
/// by SaveManager.ExecuteLoad so callers can surface specific failure reasons to players.
/// </summary>
public enum SaveLoadResult
{
    Success,

    /// <summary>No file exists at the requested slot path.</summary>
    FileNotFound,

    /// <summary>
    /// File exists but SaveVersion doesn't match SAVE_VERSION_CURRENT.
    /// Player-facing message required — not a silent failure.
    /// </summary>
    VersionMismatch,

    /// <summary>File exists and version matches, but JSON deserialization failed.</summary>
    DeserializationError,

    /// <summary>Deserialization returned a null object — file may be empty.</summary>
    NullData
}
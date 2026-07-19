using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Orchestrates save and load across all managers.
/// Serialize to JSON via Newtonsoft. One autosave + three manual slots.
/// Slot 0 = autosave. Slots 1–3 = manual.
/// </summary>
public class SaveManager : MonoBehaviour
{
    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private FinanceManager _financeManager;
    [SerializeField] private ReputationManager _reputationManager;
    [SerializeField] private EmployeeManager _employeeManager;
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private ContractManager _contractManager;
    [SerializeField] private WorldEventManager _worldEventManager;
    [SerializeField] private RivalManager _rivalManager;
    [SerializeField] private MarketManager _marketManager;

    // — Private ————————————————————————————————————————————
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        // Enums as strings — safe across enum reordering.
        Converters = new System.Collections.Generic.List<JsonConverter>
                            { new StringEnumConverter() },
        Formatting = Formatting.Indented,    // Human-readable saves — easier to debug.
        NullValueHandling = NullValueHandling.Include
    };

    private int _weeksSinceLastAutosave = 0;

    // — Unity Lifecycle ————————————————————————————————————
    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Public Methods —————————————————————————————————————

    /// <summary>Saves to the autosave slot (slot 0).</summary>
    public void SaveAutosave() => ExecuteSave(0);

    /// <summary>Saves to a manual slot (1–3).</summary>
    public void SaveManual(int slot)
    {
        if (slot < 1 || slot > AegisConstants.SAVE_MANUAL_SLOT_COUNT)
        {
            Debug.LogError($"[SaveManager] Invalid manual slot {slot}. Valid: 1–{AegisConstants.SAVE_MANUAL_SLOT_COUNT}.");
            return;
        }
        ExecuteSave(slot);
    }

    /// <summary>Loads from the autosave slot. Returns true on success.</summary>
    public SaveLoadResult LoadAutosave() => ExecuteLoad(0);

    /// <summary>Loads from a manual slot. Returns true on success.</summary>
    public SaveLoadResult LoadManual(int slot)
    {
        if (slot < 1 || slot > AegisConstants.SAVE_MANUAL_SLOT_COUNT)
        {
            Debug.LogError($"[SaveManager] Invalid slot {slot}.");
            return SaveLoadResult.FileNotFound;
        }
        return ExecuteLoad(slot);
    }

    /// <summary>Returns true if a save file exists for the given slot.</summary>
    public bool SaveExists(int slot) => File.Exists(GetSavePath(slot));

    /// <summary>
    /// Fires after every load attempt with the result and a player-readable message.
    /// Subscribe in GameHudController and future MainMenuController load screen.
    /// </summary>
    public static event Action<SaveLoadResult, string> OnLoadAttempted;

    // — Private: Core Save/Load ——————————————————————————

    private void ExecuteSave(int slot)
    {
        try
        {
            GameSaveData data = CollectSaveData();
            string json = JsonConvert.SerializeObject(data, _jsonSettings);
            string path = GetSavePath(slot);

            EnsureSaveDirectoryExists();
            File.WriteAllText(path, json);

            Debug.Log($"[SaveManager] Saved to slot {slot}: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed (slot {slot}): {e.Message}");
        }
    }

    private SaveLoadResult ExecuteLoad(int slot)
    {
        string path = GetSavePath(slot);

        if (!File.Exists(path))
        {
            string msg = $"No save file found in this slot.";
            Debug.LogWarning($"[SaveManager] Slot {slot}: file not found at {path}");
            OnLoadAttempted?.Invoke(SaveLoadResult.FileNotFound, msg);
            return SaveLoadResult.FileNotFound;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json, _jsonSettings);

            if (data == null)
            {
                string msg = "The save file could not be read.";
                Debug.LogError($"[SaveManager] Slot {slot}: deserialization returned null.");
                OnLoadAttempted?.Invoke(SaveLoadResult.NullData, msg);
                return SaveLoadResult.NullData;
            }

            // ——— Version gate — player-facing message required ————
            if (data.SaveVersion != AegisConstants.SAVE_VERSION_CURRENT)
            {
                string msg = "This save file was created with an older version of the game " +
                             "and cannot be loaded.";
                Debug.LogWarning($"[SaveManager] Slot {slot}: version mismatch. " +
                                 $"File: v{data.SaveVersion}, " +
                                 $"Expected: v{AegisConstants.SAVE_VERSION_CURRENT}.");
                OnLoadAttempted?.Invoke(SaveLoadResult.VersionMismatch, msg);
                return SaveLoadResult.VersionMismatch;
            }

            RestoreFromSaveData(data);
            OnLoadAttempted?.Invoke(SaveLoadResult.Success, $"Game loaded from slot {slot}.");
            Debug.Log($"[SaveManager] Loaded slot {slot} — Week {data.CurrentWeek}.");
            return SaveLoadResult.Success;
        }
        catch (Exception e)
        {
            string msg = "The save file is corrupted and cannot be loaded.";
            Debug.LogError($"[SaveManager] Slot {slot}: load failed — {e.Message}");
            OnLoadAttempted?.Invoke(SaveLoadResult.DeserializationError, msg);
            return SaveLoadResult.DeserializationError;
        }
    }

    // — Private: Data Collection ——————————————————————————

    private GameSaveData CollectSaveData()
    {
        var data = new GameSaveData
        {
            SaveTimestamp = DateTime.UtcNow.ToString("o")
        };

        // Order doesn't matter for save — collect from all managers.
        _timeManager?.PopulateSaveData(data);
        _financeManager?.PopulateSaveData(data);
        _reputationManager?.PopulateSaveData(data);
        _employeeManager?.PopulateSaveData(data);
        _researchManager?.PopulateSaveData(data);
        _contractManager?.PopulateSaveData(data);
        _worldEventManager?.PopulateSaveData(data);
        _rivalManager?.PopulateSaveData(data);
        _marketManager?.PopulateSaveData(data);

        return data;
    }

    /// <summary>
    /// Returns display metadata for a save slot without loading game state.
    /// Used by SettingsPanel to render save slot information.
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slot)
    {
        string path = GetSavePath(slot);
        var info = new SaveSlotInfo { Slot = slot, IsAutosave = slot == 0 };

        if (!File.Exists(path))
        {
            info.Exists = false;
            return info;
        }

        info.Exists = true;

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json, _jsonSettings);

            if (data == null) { info.IsCorrupted = true; return info; }

            info.SaveVersion = data.SaveVersion;
            info.IsCompatible = data.SaveVersion == AegisConstants.SAVE_VERSION_CURRENT;
            info.Timestamp = data.SaveTimestamp;
            info.CurrentWeek = data.CurrentWeek;
        }
        catch
        {
            info.IsCorrupted = true;
        }

        return info;
    }

    /// <summary>Returns metadata for all four slots (0 = autosave, 1–3 = manual).</summary>
    public List<SaveSlotInfo> GetAllSlotInfo()
    {
        var list = new List<SaveSlotInfo>();
        list.Add(GetSlotInfo(0));
        for (int i = 1; i <= AegisConstants.SAVE_MANUAL_SLOT_COUNT; i++)
            list.Add(GetSlotInfo(i));
        return list;
    }

    /// <summary>Permanently deletes a save slot file. Returns false if no file existed.</summary>
    public bool DeleteSlot(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        Debug.Log($"[SaveManager] Slot {slot} deleted.");
        return true;
    }

    // — Private: State Restoration ————————————————————————

    private void RestoreFromSaveData(GameSaveData data)
    {
        // ORDER MATTERS. Dependencies must be restored before dependents.
        // See M4 load order rationale in 07_Development_Log.md.

        // 1. TimeManager — sets CurrentWeek, pauses simulation.
        _timeManager?.LoadFromSaveData(data);

        // 2. Finance — sets cash balance. No manager dependencies.
        _financeManager?.LoadFromSaveData(data);

        // 3. Employees — must be loaded before Research and Contracts
        //    (both look up employees by ID during their restore).
        _employeeManager?.LoadFromSaveData(data);

        // 4. Research — looks up employees by ID for active projects.
        _researchManager?.LoadFromSaveData(data);

        // 5. Contracts — re-seeds completed research from ResearchManager.
        //    Must load after ResearchManager.
        _contractManager?.LoadFromSaveData(data);

        // 5.5 World events — must load before rivals (rivals may get event bonuses)
        _worldEventManager?.LoadFromSaveData(data);

        // 5.6 Rivals — load progress scores from save
        _rivalManager?.LoadFromSaveData(data);

        // 5.7 Market — recalculates from research + rivals, no save data dependency
        _marketManager?.LoadFromSaveData(data);

        // 6. Reputation — fires OnTierChanged last. ContractManager and
        //    EmployeeManager are loaded and ready to handle phase/tier updates.
        _reputationManager?.LoadFromSaveData(data);
    }

    // — Private: Autosave Tick ————————————————————————————

    private void HandleWeekTick()
    {
        _weeksSinceLastAutosave++;
        if (_weeksSinceLastAutosave >= AegisConstants.AUTOSAVE_INTERVAL_WEEKS)
        {
            SaveAutosave();
            _weeksSinceLastAutosave = 0;
        }
    }

    // — Private: File System ——————————————————————————————

    private static string GetSavePath(int slot)
    {
        string filename = slot == 0
            ? AegisConstants.SAVE_AUTOSAVE_FILENAME
            : string.Format(AegisConstants.SAVE_SLOT_FILENAME_FORMAT, slot);

        return Path.Combine(Application.persistentDataPath,
                            AegisConstants.SAVE_FOLDER, filename);
    }

    private static void EnsureSaveDirectoryExists()
    {
        string dir = Path.Combine(Application.persistentDataPath, AegisConstants.SAVE_FOLDER);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
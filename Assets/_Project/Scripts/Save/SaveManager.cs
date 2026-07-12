using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
    public bool LoadAutosave() => ExecuteLoad(0);

    /// <summary>Loads from a manual slot. Returns true on success.</summary>
    public bool LoadManual(int slot)
    {
        if (slot < 1 || slot > AegisConstants.SAVE_MANUAL_SLOT_COUNT)
        {
            Debug.LogError($"[SaveManager] Invalid slot {slot}.");
            return false;
        }
        return ExecuteLoad(slot);
    }

    /// <summary>Returns true if a save file exists for the given slot.</summary>
    public bool SaveExists(int slot) => File.Exists(GetSavePath(slot));

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

    private bool ExecuteLoad(int slot)
    {
        string path = GetSavePath(slot);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] No save file at slot {slot}: {path}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json, _jsonSettings);

            if (data == null)
            {
                Debug.LogError($"[SaveManager] Deserialization returned null for slot {slot}.");
                return false;
            }

            if (data.SaveVersion != 1)
            {
                Debug.LogError($"[SaveManager] Save version mismatch. " +
                               $"File: v{data.SaveVersion}, Expected: v1. Load aborted.");
                return false;
            }

            RestoreFromSaveData(data);
            Debug.Log($"[SaveManager] Loaded slot {slot} — Week {data.CurrentWeek}.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed (slot {slot}): {e.Message}");
            return false;
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

        return data;
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
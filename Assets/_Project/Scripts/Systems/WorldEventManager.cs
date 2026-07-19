using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Generates and resolves world events on a randomised cadence.
/// Exposes active modifier dictionaries consumed by ContractManager and RivalManager.
/// </summary>
public class WorldEventManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<WorldEventSO> OnEventStarted;
    public static event Action<WorldEventSO> OnEventEnded;

    // — Serialized Fields ——————————————————————————————————
    /// <summary>All WorldEventSO assets available for generation. Assign in Inspector.</summary>
    [SerializeField] private WorldEventSO[] _eventPool;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<ActiveWorldEvent> ActiveEvents => _activeEvents;

    // — Private State —————————————————————————————————————
    private readonly List<ActiveWorldEvent> _activeEvents = new();
    private int _weeksUntilNextEvent;

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        ScheduleNextEvent();
    }

    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Public: Modifier Queries ———————————————————————————

    /// <summary>
    /// Returns the combined demand multiplier for a contract category across all active events.
    /// Multipliers stack multiplicatively. 1.0 if no relevant events are active.
    /// </summary>
    public float GetDemandMultiplier(string contractCategory)
    {
        float result = 1f;
        foreach (ActiveWorldEvent active in _activeEvents)
            foreach (ContractCategoryModifier mod in active.EventSO.MarketModifiers)
                if (mod.ContractCategory == contractCategory)
                    result *= mod.DemandMultiplier;
        return result;
    }

    /// <summary>Returns the combined reward multiplier for a category across all active events.</summary>
    public float GetRewardMultiplier(string contractCategory)
    {
        float result = 1f;
        foreach (ActiveWorldEvent active in _activeEvents)
            foreach (ContractCategoryModifier mod in active.EventSO.MarketModifiers)
                if (mod.ContractCategory == contractCategory)
                    result *= mod.RewardMultiplier;
        return result;
    }

    /// <summary>Returns flat progress rate bonus for a rival from all active events combined.</summary>
    public float GetRivalProgressBonus(string rivalName)
    {
        float total = 0f;
        foreach (ActiveWorldEvent active in _activeEvents)
            foreach (RivalProgressModifier mod in active.EventSO.RivalModifiers)
                if (mod.RivalName == rivalName)
                    total += mod.ProgressRateBonus;
        return total;
    }

    // — Save / Load ————————————————————————————————————————
    public void PopulateSaveData(GameSaveData data)
    {
        data.ActiveWorldEvents = new List<ActiveWorldEventSaveData>();
        foreach (ActiveWorldEvent e in _activeEvents)
            data.ActiveWorldEvents.Add(new ActiveWorldEventSaveData
            {
                EventId = e.EventSO.EventId,
                WeeksRemaining = e.WeeksRemaining
            });

        data.WeeksUntilNextWorldEvent = _weeksUntilNextEvent;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        _activeEvents.Clear();

        if (data.ActiveWorldEvents != null)
        {
            var lookup = BuildEventLookup();
            foreach (ActiveWorldEventSaveData d in data.ActiveWorldEvents)
            {
                if (!lookup.TryGetValue(d.EventId, out WorldEventSO so))
                {
                    Debug.LogWarning($"[WorldEventManager] EventId '{d.EventId}' not found. Skipped.");
                    continue;
                }
                _activeEvents.Add(new ActiveWorldEvent { EventSO = so, WeeksRemaining = d.WeeksRemaining });
            }
        }

        _weeksUntilNextEvent = data.WeeksUntilNextWorldEvent > 0
            ? data.WeeksUntilNextWorldEvent
            : Random.Range(AegisConstants.WORLD_EVENT_MIN_CADENCE_WEEKS,
                           AegisConstants.WORLD_EVENT_MAX_CADENCE_WEEKS + 1);

        Debug.Log($"[WorldEventManager] Loaded {_activeEvents.Count} active events.");
    }

    // — Private ————————————————————————————————————————————

    private void HandleWeekTick()
    {
        TickActiveEvents();
        TickEventCadence();
    }

    private void TickActiveEvents()
    {
        var toEnd = new List<ActiveWorldEvent>();

        foreach (ActiveWorldEvent active in _activeEvents)
        {
            active.WeeksRemaining--;
            if (active.WeeksRemaining <= 0)
                toEnd.Add(active);
        }

        foreach (ActiveWorldEvent ending in toEnd)
        {
            _activeEvents.Remove(ending);
            OnEventEnded?.Invoke(ending.EventSO);
            Debug.Log($"[WorldEventManager] Event ended: {ending.EventSO.EventName}.");
        }
    }

    private void TickEventCadence()
    {
        _weeksUntilNextEvent--;

        if (_weeksUntilNextEvent > 0) return;
        if (_activeEvents.Count >= AegisConstants.WORLD_EVENT_MAX_CONCURRENT) return;
        if (_eventPool == null || _eventPool.Length == 0) return;

        TriggerRandomEvent();
        ScheduleNextEvent();
    }

    private void TriggerRandomEvent()
    {
        WorldEventSO selected = _eventPool[Random.Range(0, _eventPool.Length)];

        _activeEvents.Add(new ActiveWorldEvent
        {
            EventSO = selected,
            WeeksRemaining = selected.DurationWeeks
        });

        OnEventStarted?.Invoke(selected);
        Debug.Log($"[WorldEventManager] Event started: {selected.EventName} " +
                  $"({selected.DurationWeeks} weeks).");
    }

    private void ScheduleNextEvent()
    {
        _weeksUntilNextEvent = Random.Range(
            AegisConstants.WORLD_EVENT_MIN_CADENCE_WEEKS,
            AegisConstants.WORLD_EVENT_MAX_CADENCE_WEEKS + 1);
    }

    private Dictionary<string, WorldEventSO> BuildEventLookup()
    {
        var lookup = new Dictionary<string, WorldEventSO>();
        if (_eventPool == null) return lookup;
        foreach (WorldEventSO e in _eventPool)
            if (e != null && !string.IsNullOrEmpty(e.EventId))
                lookup[e.EventId] = e;
        return lookup;
    }
}
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the weekly simulation tick. All simulation systems subscribe to
/// OnWeekTick and advance their own state when it fires — nothing runs outside this.
/// </summary>
public class TimeManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    /// <summary>Fires every simulated week. All managers subscribe to this.</summary>
    public static event Action OnWeekTick;

    /// <summary>Fires with the new week number after CurrentWeek increments.</summary>
    public static event Action<int> OnWeekChanged;

    // — Public Properties ——————————————————————————————————
    /// <summary>Current simulation speed. 0 = paused, 1 = normal, 2 = fast, 4 = very fast.</summary>
    public float CurrentSpeed { get; private set; } = 1f;

    /// <summary>Current week number. Starts at 1.</summary>
    public int CurrentWeek { get; private set; } = 1;

    // — Private Fields ————————————————————————————————————
    private Coroutine _tickCoroutine;

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        _tickCoroutine = StartCoroutine(TickCoroutine());
        Debug.Log("[TimeManager] Simulation started at 1x speed.");
    }

    private void OnDestroy()
    {
        if (_tickCoroutine != null)
            StopCoroutine(_tickCoroutine);
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>Sets simulation speed. Valid values: 0 (pause), 1, 2, 4.</summary>
    public void SetSpeed(float speed)
    {
        if (speed != 0f && speed != 1f && speed != 2f && speed != 4f)
        {
            Debug.LogWarning($"[TimeManager] Invalid speed '{speed}'. Valid: 0, 1, 2, 4.");
            return;
        }

        CurrentSpeed = speed;
        Debug.Log($"[TimeManager] Speed set to {speed}x.");
    }

    // — Private Methods ————————————————————————————————————

    private IEnumerator TickCoroutine()
    {
        while (true)
        {
            if (CurrentSpeed > 0f)
            {
                yield return new WaitForSeconds(GetTickInterval());
                AdvanceWeek();
            }
            else
            {
                // Paused — yield one frame to avoid a busy-wait spin.
                yield return null;
            }
        }
    }

    private void AdvanceWeek()
    {
        CurrentWeek++;
        OnWeekChanged?.Invoke(CurrentWeek);
        OnWeekTick?.Invoke();
    }

    private float GetTickInterval()
    {
        return CurrentSpeed switch
        {
            1f => AegisConstants.TICK_INTERVAL_1X,
            2f => AegisConstants.TICK_INTERVAL_2X,
            4f => AegisConstants.TICK_INTERVAL_4X,
            _ => AegisConstants.TICK_INTERVAL_1X
        };
    }
}
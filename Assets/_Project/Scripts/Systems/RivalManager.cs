using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages rival corporation progress per DD-02.
/// Lightweight progress model — no full simulation. Progress scores only.
/// Feeds MarketManager for share calculation.
/// </summary>
public class RivalManager : MonoBehaviour
{
    public static event Action OnRivalProgressUpdated;

    [SerializeField] private WorldEventManager _worldEventManager;

    public IReadOnlyList<RivalProgressData> Rivals => _rivals;

    private readonly List<RivalProgressData> _rivals = new();

    private void Awake()
    {
        InitialiseRivals();
    }

    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Save / Load ————————————————————————————————————————

    public void PopulateSaveData(GameSaveData data)
    {
        data.RivalProgress = new Dictionary<string, float[]>();
        foreach (RivalProgressData rival in _rivals)
        {
            data.RivalProgress[rival.Name] = new float[]
            {
                GetProgress(rival, ResearchBranch.Drone),
                GetProgress(rival, ResearchBranch.AI),
                GetProgress(rival, ResearchBranch.Cyber),
                GetProgress(rival, ResearchBranch.Space)
            };
        }
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        if (data.RivalProgress == null) return;

        foreach (RivalProgressData rival in _rivals)
        {
            if (!data.RivalProgress.TryGetValue(rival.Name, out float[] saved)) continue;
            if (saved.Length < 4) continue;

            rival.BranchProgress[ResearchBranch.Drone] = saved[0];
            rival.BranchProgress[ResearchBranch.AI] = saved[1];
            rival.BranchProgress[ResearchBranch.Cyber] = saved[2];
            rival.BranchProgress[ResearchBranch.Space] = saved[3];
        }

        Debug.Log($"[RivalManager] Loaded {_rivals.Count} rival progress states.");
    }

    // — Private ————————————————————————————————————————————

    private void InitialiseRivals()
    {
        // Data matches lore in 05_Lore_and_Worldbuilding.md.
        // Starting progress reflects pre-game history — not starting from zero.
        _rivals.Add(CreateRival("Titan Defense", ResearchBranch.Space, isGeneralist: true,
            drone: 25f, ai: 20f, cyber: 40f, space: 45f));

        _rivals.Add(CreateRival("Nova Dynamics", ResearchBranch.AI, isGeneralist: false,
            drone: 10f, ai: 65f, cyber: 15f, space: 20f));

        _rivals.Add(CreateRival("Vanguard Robotics", ResearchBranch.Drone, isGeneralist: false,
            drone: 60f, ai: 15f, cyber: 10f, space: 20f));

        _rivals.Add(CreateRival("Helix Systems", ResearchBranch.Cyber, isGeneralist: false,
            drone: 10f, ai: 20f, cyber: 55f, space: 15f));
    }

    private static RivalProgressData CreateRival(string name, ResearchBranch spec,
        bool isGeneralist, float drone, float ai, float cyber, float space)
    {
        var rival = new RivalProgressData
        {
            Name = name,
            Specialization = spec,
            IsGeneralist = isGeneralist
        };

        rival.BranchProgress[ResearchBranch.Drone] = drone;
        rival.BranchProgress[ResearchBranch.AI] = ai;
        rival.BranchProgress[ResearchBranch.Cyber] = cyber;
        rival.BranchProgress[ResearchBranch.Space] = space;

        return rival;
    }

    private void HandleWeekTick()
    {
        foreach (RivalProgressData rival in _rivals)
            AdvanceRival(rival);

        OnRivalProgressUpdated?.Invoke();
    }

    private void AdvanceRival(RivalProgressData rival)
    {
        float eventBonus = _worldEventManager != null
            ? _worldEventManager.GetRivalProgressBonus(rival.Name)
            : 0f;

        foreach (ResearchBranch branch in System.Enum.GetValues(typeof(ResearchBranch)))
        {
            float baseRate = GetBranchRate(rival, branch);
            float newProgress = GetProgress(rival, branch) + baseRate + eventBonus;
            rival.BranchProgress[branch] = Mathf.Clamp(newProgress, 0f, 100f);
        }
    }

    private static float GetBranchRate(RivalProgressData rival, ResearchBranch branch)
    {
        if (rival.IsGeneralist)
            return AegisConstants.RIVAL_TITAN_PROGRESS_ALL;

        return branch == rival.Specialization
            ? AegisConstants.RIVAL_PROGRESS_SPECIALIZATION
            : AegisConstants.RIVAL_PROGRESS_GENERAL;
    }

    private static float GetProgress(RivalProgressData rival, ResearchBranch branch)
    {
        return rival.BranchProgress.TryGetValue(branch, out float val) ? val : 0f;
    }
}
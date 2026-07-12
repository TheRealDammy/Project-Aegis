using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns all research node states and progress. Advances research each tick
/// based on assigned researcher stats. Fires events on state changes.
/// </summary>
public class ResearchManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<ResearchNodeSO> OnResearchCompleted;
    public static event Action<string, ResearchNodeState> OnNodeStateChanged;  // nodeId, newState

    // — Serialized Fields ——————————————————————————————————
    /// <summary>All 17 research node assets. Assign in Inspector.</summary>
    [SerializeField] private ResearchNodeSO[] _allNodes;
    [SerializeField] private EmployeeManager _employeeManager;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyDictionary<string, ResearchNodeState> NodeStates => _nodeStates;
    public IReadOnlyDictionary<string, ActiveResearchProject> ActiveProjects => _activeProjects;

    // — Private State —————————————————————————————————————
    private readonly Dictionary<string, ResearchNodeState> _nodeStates = new();
    private readonly Dictionary<string, ActiveResearchProject> _activeProjects = new();

    // SO lookup — resolved once at Start. Never search _allNodes[] at runtime.
    private readonly Dictionary<string, ResearchNodeSO> _nodeById = new();

    /// <summary>
    /// Read-only access to the node SO lookup. Used by UI panels for branch/display data.
    /// ResearchManager remains the single owner of SO references.
    /// </summary>
    public IReadOnlyDictionary<string, ResearchNodeSO> NodeSOLookup => _nodeById;

    

    // — Unity Lifecycle ————————————————————————————————————
    private void Awake()
    {
        if (_allNodes == null || _allNodes.Length == 0)
        {
            Debug.LogError("[ResearchManager] No research nodes assigned in Inspector.");
            return;
        }

        // Build SO lookup table.
        foreach (var node in _allNodes)
        {
            if (node == null || string.IsNullOrEmpty(node.NodeId))
            {
                Debug.LogError("[ResearchManager] A research node has a null or empty NodeId.");
                continue;
            }

            if (_nodeById.ContainsKey(node.NodeId))
            {
                Debug.LogError($"[ResearchManager] Duplicate NodeId '{node.NodeId}'. Each node must be unique.");
                continue;
            }

            _nodeById[node.NodeId] = node;
        }
    }

    private void Start()
    {
        InitialiseNodeStates();
    }

    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Assigns a researcher to a node. Node must be Available.
    /// Also marks the employee as assigned so they don't get double-booked.
    /// </summary>
    public bool AssignResearcher(string nodeId, Employee researcher)
    {
        if (!_nodeStates.TryGetValue(nodeId, out var state))
        {
            Debug.LogWarning($"[ResearchManager] AssignResearcher: unknown node '{nodeId}'.");
            return false;
        }

        if (state != ResearchNodeState.Available)
        {
            Debug.LogWarning($"[ResearchManager] Node '{nodeId}' is not Available (state: {state}).");
            return false;
        }

        if (!string.IsNullOrEmpty(researcher.Assignment))
        {
            Debug.LogWarning($"[ResearchManager] Researcher '{researcher.Name}' is already assigned.");
            return false;
        }

        researcher.Assignment = nodeId;
        _activeProjects[nodeId] = new ActiveResearchProject
        {
            NodeId = nodeId,
            AssignedResearcherId = researcher.EmployeeId,   // Was: AssignedResearcherName
            Progress = 0f
        };

        SetNodeState(nodeId, ResearchNodeState.InProgress);
        Debug.Log($"[ResearchManager] {researcher.Name} ({researcher.EmployeeId}) " +
                  $"assigned to '{nodeId}'.");
        return true;
    }

    /// <summary>Cancels research on a node, returning it to Available state.</summary>

    public bool CancelResearch(string nodeId)
    {
        if (!_nodeStates.TryGetValue(nodeId, out ResearchNodeState state)
            || state != ResearchNodeState.InProgress)
        {
            Debug.LogWarning($"[ResearchManager] CancelResearch: '{nodeId}' is not InProgress.");
            return false;
        }

        if (_activeProjects.TryGetValue(nodeId, out ActiveResearchProject project))
        {
            UnassignResearcher(project.AssignedResearcherId);
            _activeProjects.Remove(nodeId);
        }

        SetNodeState(nodeId, ResearchNodeState.Available);
        Debug.Log($"[ResearchManager] Research cancelled: '{nodeId}'.");
        return true;
    }

    /// <summary>Returns progress 0–1 for a node. 0 if not InProgress.</summary>
    public float GetNodeProgress(string nodeId)
    {
        if (!_activeProjects.TryGetValue(nodeId, out var project)) return 0f;
        if (!_nodeById.TryGetValue(nodeId, out var so)) return 0f;
        if (so.BaseResearchCost <= 0) return 0f;

        return Mathf.Clamp01(project.Progress / so.BaseResearchCost);
    }

    /// <summary>Returns true if the node is in Complete state.</summary>
    public bool IsNodeComplete(string nodeId)
    {
        return _nodeStates.TryGetValue(nodeId, out var state) && state == ResearchNodeState.Complete;
    }

    public void PopulateSaveData(GameSaveData data)
    {
        data.CompletedResearchNodeIds = new List<string>();
        data.ActiveResearch = new List<ActiveResearchSaveData>();

        foreach (var kvp in _nodeStates)
        {
            if (kvp.Value == ResearchNodeState.Complete)
                data.CompletedResearchNodeIds.Add(kvp.Key);
        }

        foreach (var kvp in _activeProjects)
        {
            data.ActiveResearch.Add(new ActiveResearchSaveData
            {
                NodeId = kvp.Value.NodeId,
                AssignedResearcherId = kvp.Value.AssignedResearcherId,
                Progress = kvp.Value.Progress
            });
        }
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        // Step 1: Reset all nodes to Locked.
        _nodeStates.Clear();
        _activeProjects.Clear();
        foreach (string nodeId in _nodeById.Keys)
            _nodeStates[nodeId] = ResearchNodeState.Locked;

        // Step 2: Mark complete nodes.
        if (data.CompletedResearchNodeIds != null)
            foreach (string nodeId in data.CompletedResearchNodeIds)
                if (_nodeStates.ContainsKey(nodeId))
                    _nodeStates[nodeId] = ResearchNodeState.Complete;

        // Step 3: Restore active projects (InProgress state).
        if (data.ActiveResearch != null)
        {
            foreach (ActiveResearchSaveData d in data.ActiveResearch)
            {
                if (!_nodeStates.ContainsKey(d.NodeId)) continue;
                _nodeStates[d.NodeId] = ResearchNodeState.InProgress;
                _activeProjects[d.NodeId] = new ActiveResearchProject
                {
                    NodeId = d.NodeId,
                    AssignedResearcherId = d.AssignedResearcherId,
                    Progress = d.Progress
                };
            }
        }

        // Step 4: Unlock Available nodes based on completed prerequisites.
        // This correctly sets nodes whose prerequisites are now met.
        UnlockEligibleNodes();

        // Step 5: Fire state change events so UI panels refresh.
        foreach (var kvp in _nodeStates)
            OnNodeStateChanged?.Invoke(kvp.Key, kvp.Value);

        Debug.Log($"[ResearchManager] Loaded. Complete: {data.CompletedResearchNodeIds?.Count ?? 0}, " +
                  $"In Progress: {_activeProjects.Count}.");
    }

    // — Private Methods ————————————————————————————————————

    private void InitialiseNodeStates()
    {
        foreach (var kvp in _nodeById)
        {
            string nodeId = kvp.Key;
            ResearchNodeSO node = kvp.Value;

            // Root nodes (no prerequisites) start as Available.
            // All others start Locked.
            bool isRoot = node.Prerequisites == null || node.Prerequisites.Length == 0;
            _nodeStates[nodeId] = isRoot ? ResearchNodeState.Available : ResearchNodeState.Locked;
        }

        Debug.Log($"[ResearchManager] Initialised {_nodeStates.Count} nodes. " +
                  $"Available: {CountNodesInState(ResearchNodeState.Available)}");
    }

    private void HandleWeekTick()
    {
        AdvanceInProgressNodes();
        UnlockEligibleNodes();
    }

    private void AdvanceInProgressNodes()
    {
        var completed = new List<string>();

        foreach (var kvp in _activeProjects)
        {
            string nodeId = kvp.Key;
            ActiveResearchProject project = kvp.Value;

            if (!_nodeById.TryGetValue(nodeId, out ResearchNodeSO so)) continue;

            project.Progress += GetResearcherProgressThisTick(project.AssignedResearcherId);

            if (project.Progress >= so.BaseResearchCost)
                completed.Add(nodeId);
        }

        foreach (string nodeId in completed)
            CompleteNode(nodeId);
    }

    /// <summary>
    /// Returns progress units for one tick based on the assigned researcher's stats.
    /// Falls back to RESEARCH_PROGRESS_PER_TICK if the researcher can't be found.
    /// A researcher with ResearchSpeed = RESEARCH_SPEED_BASELINE progresses at 1.0× per tick.
    /// </summary>
    private float GetResearcherProgressThisTick(string researcherId)   // param type unchanged, name clarified
    {
        if (_employeeManager == null || string.IsNullOrEmpty(researcherId))
            return AegisConstants.RESEARCH_PROGRESS_PER_TICK;

        Employee researcher = _employeeManager.GetEmployeeById(researcherId);   // Was: GetEmployeeByName
        if (researcher == null)
        {
            Debug.LogWarning($"[ResearchManager] Researcher ID '{researcherId}' not found on roster.");
            return AegisConstants.RESEARCH_PROGRESS_PER_TICK;
        }

        float speed = researcher.GetModifiedStat(AegisConstants.STAT_RESEARCH_SPEED);
        return speed / AegisConstants.RESEARCH_SPEED_BASELINE;
    }

    private void CompleteNode(string nodeId)
    {
        // Unassign the researcher before removing the project record.
        if (_activeProjects.TryGetValue(nodeId, out ActiveResearchProject project))
            UnassignResearcher(project.AssignedResearcherId);

        _activeProjects.Remove(nodeId);
        SetNodeState(nodeId, ResearchNodeState.Complete);

        if (_nodeById.TryGetValue(nodeId, out ResearchNodeSO so))
        {
            OnResearchCompleted?.Invoke(so);
            Debug.Log($"[ResearchManager] Research complete: '{so.DisplayName}'.");
        }
    }

    private void UnassignResearcher(string researcherId)
    {
        if (_employeeManager == null || string.IsNullOrEmpty(researcherId)) return;
        Employee researcher = _employeeManager.GetEmployeeById(researcherId);   // Was: GetEmployeeByName
        if (researcher != null)
            researcher.Assignment = null;
    }

    private void UnlockEligibleNodes()
    {
        foreach (var kvp in _nodeById)
        {
            string nodeId = kvp.Key;
            ResearchNodeSO node = kvp.Value;

            if (_nodeStates[nodeId] != ResearchNodeState.Locked) continue;

            if (AllPrerequisitesMet(node))
                SetNodeState(nodeId, ResearchNodeState.Available);
        }
    }

    private bool AllPrerequisitesMet(ResearchNodeSO node)
    {
        if (node.Prerequisites == null || node.Prerequisites.Length == 0) return true;

        foreach (var prereq in node.Prerequisites)
        {
            if (prereq == null) continue;
            if (!IsNodeComplete(prereq.NodeId)) return false;
        }

        return true;
    }

    private void SetNodeState(string nodeId, ResearchNodeState newState)
    {
        _nodeStates[nodeId] = newState;
        OnNodeStateChanged?.Invoke(nodeId, newState);
    }

    private int CountNodesInState(ResearchNodeState state)
    {
        int count = 0;
        foreach (var s in _nodeStates.Values)
            if (s == state) count++;
        return count;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Builds and refreshes the Research panel VisualElement tree.
/// Plain C# class — not a MonoBehaviour. Owned by GameHudController.
/// Events bubble to GameHudController for cross-manager coordination.
/// </summary>
public class ResearchPanel
{
    // — Events ——————————————————————————————————————————————
    public event Action<string> OnAssignResearcherRequested;  // nodeId
    public event Action<string> OnCancelResearchRequested;    // nodeId

    // — Dependencies ————————————————————————————————————————
    private readonly VisualElement _container;
    private readonly ResearchManager _researchManager;
    private readonly EmployeeManager _employeeManager;

    // nodeId → card root element, for targeted refresh.
    private readonly Dictionary<string, VisualElement> _nodeCards = new();
    private readonly Dictionary<string, Label> _nodeStatusLabels = new();
    private readonly Dictionary<string, VisualElement> _nodeProgressFills = new();

    // — Constructor —————————————————————————————————————————
    public ResearchPanel(VisualElement container, ResearchManager researchManager,
                         EmployeeManager employeeManager)
    {
        _container = container;
        _researchManager = researchManager;
        _employeeManager = employeeManager;
    }

    // — Public ——————————————————————————————————————————————

    public void Build()
    {
        _container.Clear();
        _nodeCards.Clear();
        _nodeStatusLabels.Clear();
        _nodeProgressFills.Clear();

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1f;
        row.style.paddingTop = 0;
        _container.Add(row);

        foreach (ResearchBranch branch in Enum.GetValues(typeof(ResearchBranch)))
            row.Add(BuildBranchColumn(branch));
    }

    public void Refresh()
    {
        // Refresh node cards.
        foreach (var kvp in _nodeCards)
            RefreshNodeCard(kvp.Key, kvp.Value);

        // Refresh connectors — stored as VEs with userData = childNodeId.
        foreach (VisualElement child in GetAllConnectors())
        {
            if (child.userData is string nodeId)
                UpdateConnectorState(child, nodeId);
        }
    }

    private IEnumerable<VisualElement> GetAllConnectors()
    {
        // Walk all branch columns and collect connector VEs.
        var connectors = new List<VisualElement>();
        if (_container == null) return connectors;
        CollectConnectors(_container, connectors);
        return connectors;
    }

    private static void CollectConnectors(VisualElement root, List<VisualElement> results)
    {
        foreach (VisualElement child in root.Children())
        {
            if (child.ClassListContains(AegisConstants.USS_NODE_CONNECTOR))
                results.Add(child);
            CollectConnectors(child, results);
        }
    }

    // — Private: Column —————————————————————————————————————

    private VisualElement BuildBranchColumn(ResearchBranch branch)
    {
        var column = new VisualElement();
        column.AddToClassList(AegisConstants.USS_BRANCH_COLUMN);

        var header = new Label(branch.ToString().ToUpper());
        header.AddToClassList(AegisConstants.USS_BRANCH_HEADER);
        column.Add(header);

        // Build an ordered list of nodes for this branch (root → tip order).
        var branchNodes = GetOrderedBranchNodes(branch);

        for (int i = 0; i < branchNodes.Count; i++)
        {
            ResearchNodeSO node = branchNodes[i];
            var card = BuildNodeCard(node);
            _nodeCards[node.NodeId] = card;
            column.Add(card);

            // Add connector AFTER each node except the last.
            if (i < branchNodes.Count - 1)
            {
                ResearchNodeSO childNode = branchNodes[i + 1];
                var connector = BuildConnector(childNode.NodeId);
                column.Add(connector);
            }
        }

        return column;
    }



    // — Private: Node Card ——————————————————————————————————

    /// <summary>
    /// Returns nodes for a branch in tree order (root first, tip last).
    /// Sorted by prerequisite depth: root nodes have depth 0, each step adds 1.
    /// </summary>
    private List<ResearchNodeSO> GetOrderedBranchNodes(ResearchBranch branch)
    {
        var nodes = new List<ResearchNodeSO>();
        foreach (var kvp in _researchManager.NodeSOLookup)
            if (kvp.Value.Branch == branch)
                nodes.Add(kvp.Value);

        // Sort by depth (number of prerequisites in chain).
        nodes.Sort((a, b) => GetNodeDepth(a).CompareTo(GetNodeDepth(b)));
        return nodes;
    }

    private int GetNodeDepth(ResearchNodeSO node)
    {
        if (node.Prerequisites == null || node.Prerequisites.Length == 0) return 0;
        int maxPrereqDepth = 0;
        foreach (var prereq in node.Prerequisites)
            if (prereq != null)
                maxPrereqDepth = Mathf.Max(maxPrereqDepth, GetNodeDepth(prereq));
        return maxPrereqDepth + 1;
    }

    private VisualElement BuildConnector(string childNodeId)
    {
        var connector = new VisualElement();
        connector.AddToClassList(AegisConstants.USS_NODE_CONNECTOR);

        // Store nodeId to allow targeted refresh of connector state.
        connector.userData = childNodeId;

        // Set initial active state based on child node state.
        UpdateConnectorState(connector, childNodeId);

        return connector;
    }

    private void UpdateConnectorState(VisualElement connector, string childNodeId)
    {
        if (!_researchManager.NodeStates.TryGetValue(childNodeId, out ResearchNodeState state)) return;

        bool active = state != ResearchNodeState.Locked;
        connector.EnableInClassList(AegisConstants.USS_NODE_CONNECTOR_ACTIVE, active);
    }

    private VisualElement BuildNodeCard(ResearchNodeSO node)
    {
        var card = new VisualElement();
        card.AddToClassList(AegisConstants.USS_NODE_CARD);

        var nameLabel = new Label(node.DisplayName);
        nameLabel.AddToClassList(AegisConstants.USS_NODE_NAME);
        card.Add(nameLabel);

        var statusLabel = new Label();
        statusLabel.AddToClassList(AegisConstants.USS_NODE_STATUS);
        card.Add(statusLabel);
        _nodeStatusLabels[node.NodeId] = statusLabel;

        // Progress bar — hidden by default, shown when InProgress.
        var progressBar = new VisualElement();
        progressBar.AddToClassList(AegisConstants.USS_NODE_PROGRESS_BAR);
        progressBar.style.display = DisplayStyle.None;

        var progressFill = new VisualElement();
        progressFill.AddToClassList(AegisConstants.USS_NODE_PROGRESS_FILL);
        progressFill.style.width = Length.Percent(0f);
        progressBar.Add(progressFill);
        card.Add(progressBar);
        _nodeProgressFills[node.NodeId] = progressFill;

        // Store progress bar ref on the card via userData for refresh access.
        card.userData = progressBar;

        // Assign button — only shown for Available nodes.
        var assignBtn = new Button();
        assignBtn.AddToClassList(AegisConstants.USS_NODE_ASSIGN_BTN);
        assignBtn.style.display = DisplayStyle.None;

        string capturedNodeId = node.NodeId; // Closure capture.
        assignBtn.clicked += () => OnAssignResearcherRequested?.Invoke(capturedNodeId);
        card.Add(assignBtn);

        // Initial state render.
        ApplyNodeState(node.NodeId, card, statusLabel, progressBar, progressFill, assignBtn);

        return card;
    }

    private void RefreshNodeCard(string nodeId, VisualElement card)
    {
        if (!_nodeStatusLabels.TryGetValue(nodeId, out var statusLabel)) return;

        var progressBar = card.userData as VisualElement;
        var progressFill = progressBar != null && progressBar.childCount > 0
            ? progressBar[0] : null;

        // Find assign button — it's always the last child.
        Button assignBtn = null;
        for (int i = card.childCount - 1; i >= 0; i--)
        {
            if (card[i] is Button btn) { assignBtn = btn; break; }
        }

        ApplyNodeState(nodeId, card, statusLabel, progressBar, progressFill, assignBtn);
    }

    private void ApplyNodeState(
        string nodeId,
        VisualElement card,
        Label statusLabel,
        VisualElement progressBar,
        VisualElement progressFill,
        Button assignBtn)
    {
        if (!_researchManager.NodeStates.TryGetValue(nodeId, out var state)) return;

        // Remove all state classes before applying the correct one.
        card.RemoveFromClassList(AegisConstants.USS_NODE_LOCKED);
        card.RemoveFromClassList(AegisConstants.USS_NODE_AVAILABLE);
        card.RemoveFromClassList(AegisConstants.USS_NODE_IN_PROGRESS);
        card.RemoveFromClassList(AegisConstants.USS_NODE_COMPLETE);

        switch (state)
        {
            case ResearchNodeState.Locked:
                card.AddToClassList(AegisConstants.USS_NODE_LOCKED);
                statusLabel.text = "LOCKED";
                if (progressBar != null) progressBar.style.display = DisplayStyle.None;
                if (assignBtn != null) assignBtn.style.display = DisplayStyle.None;
                break;

            case ResearchNodeState.Available:
                card.AddToClassList(AegisConstants.USS_NODE_AVAILABLE);
                statusLabel.text = "AVAILABLE";
                if (progressBar != null) progressBar.style.display = DisplayStyle.None;
                if (assignBtn != null)
                {
                    assignBtn.style.display = DisplayStyle.Flex;
                    bool hasResearcher = HasUnassignedResearcher();
                    assignBtn.text = hasResearcher ? "ASSIGN RESEARCHER" : "NO RESEARCHER";
                    assignBtn.SetEnabled(hasResearcher);
                }
                break;

            case ResearchNodeState.InProgress:
                card.AddToClassList(AegisConstants.USS_NODE_IN_PROGRESS);
                float progress = _researchManager.GetNodeProgress(nodeId);
                statusLabel.text = $"IN PROGRESS — {progress * 100f:F0}%";
                if (progressBar != null) progressBar.style.display = DisplayStyle.Flex;
                if (progressFill != null) progressFill.style.width = Length.Percent(progress * 100f);
                if (assignBtn != null) assignBtn.style.display = DisplayStyle.None;
                break;

            case ResearchNodeState.Complete:
                card.AddToClassList(AegisConstants.USS_NODE_COMPLETE);
                statusLabel.text = "COMPLETE";
                if (progressBar != null) progressBar.style.display = DisplayStyle.None;
                if (assignBtn != null) assignBtn.style.display = DisplayStyle.None;
                break;
        }
    }

    private bool HasUnassignedResearcher()
    {
        foreach (var emp in _employeeManager.Employees)
        {
            if (emp.Role == EmployeeRole.Researcher && string.IsNullOrEmpty(emp.Assignment))
                return true;
        }
        return false;
    }
}
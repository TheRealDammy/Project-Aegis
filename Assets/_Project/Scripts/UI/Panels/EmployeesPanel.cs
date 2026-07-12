using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Builds the Employees panel — hiring pool on the left, roster on the right.
/// Plain C# class owned by GameHudController.
/// </summary>
public class EmployeesPanel
{
    public event Action<HiringCandidate> OnHireRequested;

    private readonly VisualElement _container;
    private readonly EmployeeManager _employeeManager;

    private VisualElement _poolColumn;
    private VisualElement _rosterColumn;

    public EmployeesPanel(VisualElement container, EmployeeManager employeeManager)
    {
        _container = container;
        _employeeManager = employeeManager;
    }

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("employees-panel");
        _container.Add(panel);

        // Left: hiring pool
        _poolColumn = new VisualElement();
        _poolColumn.AddToClassList("emp-column");
        _poolColumn.AddToClassList("emp-column--left");
        panel.Add(_poolColumn);

        // Right: roster
        _rosterColumn = new VisualElement();
        _rosterColumn.AddToClassList("emp-column");
        panel.Add(_rosterColumn);

        Refresh();
    }

    public void Refresh()
    {
        RefreshPool();
        RefreshRoster();
    }

    // — Pool ————————————————————————————————————————————

    private void RefreshPool()
    {
        _poolColumn.Clear();

        var header = new Label("HIRING POOL");
        header.AddToClassList("emp-section-header");
        _poolColumn.Add(header);

        if (_employeeManager.HiringPool.Count == 0)
        {
            AddDetail(_poolColumn, "No candidates available.");
            return;
        }

        foreach (HiringCandidate candidate in _employeeManager.HiringPool)
            _poolColumn.Add(BuildCandidateCard(candidate));
    }

    private VisualElement BuildCandidateCard(HiringCandidate candidate)
    {
        var card = new VisualElement();
        card.AddToClassList("emp-card");
        card.AddToClassList(candidate.WeeksAvailable <= 1
            ? "emp-card--expiring"
            : "emp-card--candidate");

        var header = new VisualElement();
        header.AddToClassList("emp-card-header");

        var nameLabel = new Label(candidate.Name);
        nameLabel.AddToClassList("emp-card-name");

        var roleLabel = new Label(candidate.Role.ToString().ToUpper());
        roleLabel.AddToClassList("emp-card-role");

        header.Add(nameLabel);
        header.Add(roleLabel);
        card.Add(header);

        // Show primary stat relevant to the role.
        string primaryStatValue = GetPrimaryStatDisplay(candidate.Role, candidate.Stats);
        AddDetail(card, $"Stats: {primaryStatValue}   Salary: £{candidate.WeeklySalary:N0}/wk");

        var expiryLabel = new Label($"Available for: {candidate.WeeksAvailable} week(s)");
        expiryLabel.AddToClassList(candidate.WeeksAvailable <= 1
            ? "emp-card-expiry--urgent"
            : "emp-card-expiry");
        card.Add(expiryLabel);

        var hireBtn = new Button();
        hireBtn.AddToClassList("emp-hire-btn");
        hireBtn.text = "HIRE";

        HiringCandidate captured = candidate;
        hireBtn.clicked += () => OnHireRequested?.Invoke(captured);
        card.Add(hireBtn);

        return card;
    }

    // — Roster ——————————————————————————————————————————

    private void RefreshRoster()
    {
        _rosterColumn.Clear();

        var header = new Label("CURRENT ROSTER");
        header.AddToClassList("emp-section-header");
        _rosterColumn.Add(header);

        if (_employeeManager.Employees.Count == 0)
        {
            AddDetail(_rosterColumn, "No employees hired.");
            return;
        }

        foreach (Employee emp in _employeeManager.Employees)
            _rosterColumn.Add(BuildRosterCard(emp));
    }

    private VisualElement BuildRosterCard(Employee emp)
    {
        bool isAssigned = !string.IsNullOrEmpty(emp.Assignment);

        var card = new VisualElement();
        card.AddToClassList("emp-card");
        card.AddToClassList(isAssigned ? "emp-card--assigned" : "emp-card--rostered");

        var header = new VisualElement();
        header.AddToClassList("emp-card-header");

        var nameLabel = new Label(emp.Name);
        nameLabel.AddToClassList("emp-card-name");

        var roleLabel = new Label(emp.Role.ToString().ToUpper());
        roleLabel.AddToClassList("emp-card-role");

        header.Add(nameLabel);
        header.Add(roleLabel);
        card.Add(header);

        AddDetail(card, $"Salary: £{emp.WeeklySalary:N0}/wk   " +
                        $"Happiness: {emp.Happiness:F0}");

        string assignmentText = isAssigned
            ? $"Assigned: {emp.Assignment}"
            : "Unassigned — available";
        AddDetail(card, assignmentText);

        return card;
    }

    // — Helpers ——————————————————————————————————————————

    private static string GetPrimaryStatDisplay(EmployeeRole role,
                                                 System.Collections.Generic.Dictionary<string, float> stats)
    {
        string statKey = role switch
        {
            EmployeeRole.Engineer => AegisConstants.STAT_EFFICIENCY,
            EmployeeRole.Researcher => AegisConstants.STAT_RESEARCH_SPEED,
            EmployeeRole.SalesManager => AegisConstants.STAT_NEGOTIATION,
            EmployeeRole.Executive => AegisConstants.STAT_LEADERSHIP,
            _ => AegisConstants.STAT_EFFICIENCY
        };

        if (stats != null && stats.TryGetValue(statKey, out float value))
            return $"{statKey}: {value:F0}";

        return "—";
    }

    private static void AddDetail(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("emp-card-detail");
        parent.Add(label);
    }
}
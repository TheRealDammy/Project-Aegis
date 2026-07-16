using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Builds the Employees panel — candidate hiring pool table and roster assignment table.
/// Layout follows 04_Art_Direction.md: alternating rows, monospace numbers, Label-size headers.
/// Plain C# class owned by GameHudController.
/// </summary>
public class EmployeesPanel
{
    // — Events — bubbled to GameHudController ——————————————
    public event Action<HiringCandidate> OnHireRequested;
    public event Action<Employee, string> OnAssignToContractRequested;  // employee, contractId

    // — Dependencies ————————————————————————————————————————
    private readonly VisualElement _container;
    private readonly EmployeeManager _employeeManager;
    private readonly ContractManager _contractManager;

    private VisualElement _poolSection;
    private VisualElement _rosterSection;

    // — Constructor —————————————————————————————————————————
    public EmployeesPanel(VisualElement container, EmployeeManager employeeManager,
                          ContractManager contractManager)
    {
        _container = container;
        _employeeManager = employeeManager;
        _contractManager = contractManager;
    }

    // — Public ——————————————————————————————————————————————

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("emp-panel");
        _container.Add(panel);

        // Hiring pool section
        _poolSection = new VisualElement();
        panel.Add(_poolSection);

        // Roster section
        _rosterSection = new VisualElement();
        panel.Add(_rosterSection);

        Refresh();
    }

    public void Refresh()
    {
        BuildPoolSection();
        BuildRosterSection();
    }

    // — Pool Section ————————————————————————————————————————

    private void BuildPoolSection()
    {
        _poolSection.Clear();

        AddSectionLabel(_poolSection, "HIRING POOL");

        if (_employeeManager.HiringPool.Count == 0)
        {
            AddEmptyRow(_poolSection, "No candidates available this week.");
            return;
        }

        // Column headers
        _poolSection.Add(BuildPoolHeaderRow());

        // Candidate rows
        int index = 0;
        foreach (HiringCandidate candidate in _employeeManager.HiringPool)
        {
            _poolSection.Add(BuildCandidateRow(candidate, index));
            index++;
        }
    }

    private VisualElement BuildPoolHeaderRow()
    {
        var row = new VisualElement();
        row.AddToClassList("table-header-row");

        AddHeaderCell(row, "NAME", "emp-col-name");
        AddHeaderCell(row, "ROLE", "emp-col-role");
        AddHeaderCell(row, "KEY STAT", "emp-col-stat");
        AddHeaderCell(row, "TRAITS", "emp-col-traits");
        AddHeaderCell(row, "SALARY/WK", "emp-col-salary");
        AddHeaderCell(row, "EXPIRES", "emp-col-expiry");
        AddHeaderCell(row, "", "emp-col-action");

        return row;
    }

    private VisualElement BuildCandidateRow(HiringCandidate candidate, int index)
    {
        var row = new VisualElement();
        row.AddToClassList("table-row");
        row.AddToClassList(index % 2 == 0 ? "table-row--even" : "table-row--odd");

        // Name
        AddTextCell(row, candidate.Name, "emp-col-name", "cell-text-primary");

        // Role
        AddTextCell(row, candidate.Role.ToString(), "emp-col-role", "cell-text-secondary");

        // Key stat — trait-modified value via GetModifiedStat
        string statKey = GetPrimaryStatKey(candidate.Role);
        float statValue = GetModifiedStatFromCandidate(candidate, statKey);
        AddTextCell(row, $"{statKey.ToUpper()}: {statValue:F0}", "emp-col-stat", "cell-text-data");

        // Traits — names joined, "—" if none
        string traitText = candidate.Traits.Count > 0
            ? string.Join(", ", candidate.Traits.ConvertAll(t => t.DisplayName))
            : "—";
        AddTextCell(row, traitText, "emp-col-traits", "cell-text-secondary");

        // Salary — monospace, right-aligned per art direction
        var salaryCell = AddTextCell(row, $"£{candidate.WeeklySalary:N0}",
                                     "emp-col-salary", "cell-text-data");
        salaryCell.style.unityTextAlign = TextAnchor.MiddleRight;

        // Expiry — amber when 1 week remaining
        string expiryClass = candidate.WeeksAvailable <= 1
            ? "cell-text-warning"
            : "cell-text-secondary";
        AddTextCell(row, $"{candidate.WeeksAvailable}w", "emp-col-expiry", expiryClass);

        // Hire button
        var actionCell = new VisualElement();
        actionCell.AddToClassList("emp-col-action");
        actionCell.style.alignItems = Align.Center;
        actionCell.style.justifyContent = Justify.Center;

        var hireBtn = new Button();
        hireBtn.AddToClassList("emp-hire-btn");
        hireBtn.text = "HIRE";

        HiringCandidate captured = candidate;
        hireBtn.clicked += () => OnHireRequested?.Invoke(captured);
        actionCell.Add(hireBtn);
        row.Add(actionCell);

        return row;
    }

    // — Roster Section ——————————————————————————————————————

    private void BuildRosterSection()
    {
        _rosterSection.Clear();

        AddSectionLabel(_rosterSection, "ROSTER & ASSIGNMENT");

        if (_employeeManager.Employees.Count == 0)
        {
            AddEmptyRow(_rosterSection, "No employees hired. Use the pool above.");
            return;
        }

        _rosterSection.Add(BuildRosterHeaderRow());

        int index = 0;
        foreach (Employee emp in _employeeManager.Employees)
        {
            _rosterSection.Add(BuildRosterRow(emp, index));
            index++;
        }
    }

    private VisualElement BuildRosterHeaderRow()
    {
        var row = new VisualElement();
        row.AddToClassList("table-header-row");

        AddHeaderCell(row, "NAME", "emp-col-name");
        AddHeaderCell(row, "ROLE", "emp-col-role");
        AddHeaderCell(row, "KEY STAT", "emp-col-stat");
        AddHeaderCell(row, "SALARY/WK", "emp-col-salary");
        AddHeaderCell(row, "ASSIGN TO CONTRACT", "emp-col-action");
        // emp-col-action is wider — assignment control needs space

        return row;
    }

    private VisualElement BuildRosterRow(Employee emp, int index)
    {
        var row = new VisualElement();
        row.AddToClassList("table-row");
        row.AddToClassList(index % 2 == 0 ? "table-row--even" : "table-row--odd");

        // Name
        AddTextCell(row, emp.Name, "emp-col-name", "cell-text-primary");

        // Role
        AddTextCell(row, emp.Role.ToString(), "emp-col-role", "cell-text-secondary");

        // Key stat — trait-modified
        string statKey = GetPrimaryStatKey(emp.Role);
        float statValue = emp.GetModifiedStat(statKey);
        AddTextCell(row, $"{statKey.ToUpper()}: {statValue:F0}", "emp-col-stat", "cell-text-data");

        // Salary
        var salaryCell = AddTextCell(row, $"£{emp.WeeklySalary:N0}",
                                     "emp-col-salary", "cell-text-data");
        salaryCell.style.unityTextAlign = TextAnchor.MiddleRight;

        // Assignment control
        var actionCell = new VisualElement();
        actionCell.AddToClassList("emp-col-action");
        actionCell.style.flexDirection = FlexDirection.Row;
        actionCell.style.alignItems = Align.Center;
        actionCell.style.flexGrow = 1f;
        actionCell.style.paddingRight = 4f;

        if (!string.IsNullOrEmpty(emp.Assignment))
        {
            // Already assigned — show assignment and unassign option
            var assignedLabel = new Label($"→ {emp.Assignment}");
            assignedLabel.AddToClassList("cell-text-success");
            assignedLabel.style.flexGrow = 1f;
            actionCell.Add(assignedLabel);
        }
        else
        {
            BuildAssignmentControl(actionCell, emp);
        }

        row.Add(actionCell);
        return row;
    }

    private void BuildAssignmentControl(VisualElement parent, Employee emp)
    {
        // Only Engineers can be assigned to contracts.
        if (emp.Role != EmployeeRole.Engineer)
        {
            var na = new Label("—");
            na.AddToClassList("cell-text-dim");
            parent.Add(na);
            return;
        }

        var activeContracts = GetAssignableContracts();

        if (activeContracts.Count == 0)
        {
            var none = new Label("No active contracts");
            none.AddToClassList("cell-text-secondary");
            parent.Add(none);
            return;
        }

        // Dropdown listing active contracts by ID + category
        var choices = new List<string>();
        var contractIds = new List<string>();
        foreach (Contract c in activeContracts)
        {
            choices.Add($"{c.ContractId} — {c.ContractCategory}");
            contractIds.Add(c.ContractId);
        }

        var dropdown = new DropdownField(choices, 0);
        dropdown.AddToClassList("emp-assign-dropdown");
        parent.Add(dropdown);

        var assignBtn = new Button();
        assignBtn.AddToClassList("emp-assign-btn");
        assignBtn.text = "ASSIGN";

        Employee capturedEmp = emp;
        assignBtn.clicked += () =>
        {
            int idx = dropdown.index;
            if (idx >= 0 && idx < contractIds.Count)
                OnAssignToContractRequested?.Invoke(capturedEmp, contractIds[idx]);
        };

        parent.Add(assignBtn);
    }

    // — Helpers ——————————————————————————————————————————————

    private List<Contract> GetAssignableContracts()
    {
        var result = new List<Contract>();
        foreach (Contract c in _contractManager.ActiveContracts)
            result.Add(c);
        return result;
    }

    private static string GetPrimaryStatKey(EmployeeRole role) => role switch
    {
        EmployeeRole.Engineer => AegisConstants.STAT_EFFICIENCY,
        EmployeeRole.Researcher => AegisConstants.STAT_RESEARCH_SPEED,
        EmployeeRole.SalesManager => AegisConstants.STAT_NEGOTIATION,
        EmployeeRole.Executive => AegisConstants.STAT_LEADERSHIP,
        _ => AegisConstants.STAT_EFFICIENCY
    };

    /// <summary>
    /// Returns the trait-modified stat value from a HiringCandidate.
    /// Mirrors Employee.GetModifiedStat() for candidates not yet on the roster.
    /// </summary>
    private static float GetModifiedStatFromCandidate(HiringCandidate candidate, string statName)
    {
        if (!candidate.Stats.TryGetValue(statName, out float baseValue)) return 0f;

        float modified = baseValue;
        foreach (TraitSO trait in candidate.Traits)
            foreach (StatModifier mod in trait.StatModifiers)
                if (mod.StatName == statName) modified += mod.Value;

        return Mathf.Clamp(modified, 0f, 100f);
    }

    // — Section structure helpers ————————————————————————————

    private static void AddSectionLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("emp-section-label");
        parent.Add(label);
    }

    private static void AddEmptyRow(VisualElement parent, string message)
    {
        var row = new VisualElement();
        row.AddToClassList("table-row");
        row.AddToClassList("table-row--even");
        var label = new Label(message);
        label.AddToClassList("cell-text-secondary");
        row.Add(label);
        parent.Add(row);
    }

    private static void AddHeaderCell(VisualElement row, string text, string sizeClass)
    {
        var cell = new Label(text);
        cell.AddToClassList("table-header-cell");
        cell.AddToClassList(sizeClass);
        row.Add(cell);
    }

    /// <summary>Adds a text data cell and returns the label for further style adjustments.</summary>
    private static Label AddTextCell(VisualElement row, string text,
                                     string sizeClass, string styleClass)
    {
        var cell = new Label(text);
        cell.AddToClassList(styleClass);
        cell.AddToClassList(sizeClass);
        row.Add(cell);
        return cell;
    }
}
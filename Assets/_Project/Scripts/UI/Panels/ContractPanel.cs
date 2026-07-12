using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Builds and refreshes the Contracts panel.
/// Shows available offers with Accept buttons and active contracts with progress.
/// Plain C# class owned by GameHudController — not a MonoBehaviour.
/// </summary>
public class ContractPanel
{
    // — Events — bubble to GameHudController ———————————————
    public event Action<Contract> OnContractAcceptRequested;

    // — Dependencies ————————————————————————————————————————
    private readonly VisualElement _container;
    private readonly ContractManager _contractManager;
    private readonly EmployeeManager _employeeManager;

    // — Internal State ————————————————————————————————————
    private VisualElement _availableSection;
    private VisualElement _activeSection;

    // — Constructor —————————————————————————————————————————
    public ContractPanel(VisualElement container, ContractManager contractManager,
                         EmployeeManager employeeManager)
    {
        _container = container;
        _contractManager = contractManager;
        _employeeManager = employeeManager;
    }

    // — Private: State for engineer selection modal —————————————
    private Contract _pendingContract;
    private VisualElement _engineerSelectionView;
    private VisualElement _mainView;
    private readonly System.Collections.Generic.HashSet<string> _selectedEngineerIds
        = new System.Collections.Generic.HashSet<string>();

    // — Public ——————————————————————————————————————————————

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("contract-panel");
        _container.Add(panel);

        var availableHeader = new Label("AVAILABLE CONTRACTS");
        availableHeader.AddToClassList("contract-section-header");
        panel.Add(availableHeader);

        _availableSection = new VisualElement();
        panel.Add(_availableSection);

        var activeHeader = new Label("ACTIVE CONTRACTS");
        activeHeader.AddToClassList("contract-section-header");
        panel.Add(activeHeader);

        _activeSection = new VisualElement();
        panel.Add(_activeSection);

        Refresh();
    }

    public void Refresh()
    {
        RefreshAvailable();
        RefreshActive();
    }

    // — Private: Available ————————————————————————————————

    private void RefreshAvailable()
    {
        _availableSection.Clear();

        if (_contractManager.AvailableContracts.Count == 0)
        {
            AddEmptyLabel(_availableSection, "No contracts available.");
            return;
        }

        foreach (Contract contract in _contractManager.AvailableContracts)
            _availableSection.Add(BuildAvailableCard(contract));
    }

    private VisualElement BuildAvailableCard(Contract contract)
    {
        var card = new VisualElement();
        card.AddToClassList("contract-card");
        card.AddToClassList("contract-card--available");

        var info = new VisualElement();
        info.AddToClassList("contract-card-info");

        var title = new Label(contract.ContractCategory);
        title.AddToClassList("contract-card-title");
        info.Add(title);

        var detail = new Label(
            $"Reward: £{contract.BaseRewardGBP:N0}   " +
            $"Deadline: {contract.DeadlineWeeks} weeks   " +
            $"Min Tier: {contract.ReputationTierRequired}");
        detail.AddToClassList("contract-card-detail");
        info.Add(detail);

        // Preview risk based on currently available engineers.
        float previewRisk = GetPreviewRisk(contract);
        var riskLabel = new Label($"Estimated risk: {previewRisk:F1}% success");
        riskLabel.AddToClassList("contract-card-risk");
        ApplyRiskColourClass(riskLabel, previewRisk);
        info.Add(riskLabel);

        card.Add(info);

        var acceptBtn = new Button();
        acceptBtn.AddToClassList("contract-accept-btn");
        acceptBtn.text = "ASSIGN & ACCEPT";

        Contract captured = contract;
        acceptBtn.clicked += () => ShowEngineerSelection(captured);
        card.Add(acceptBtn);

        return card;
    }

    // — Private: Active ———————————————————————————————————

    private void RefreshActive()
    {
        _activeSection.Clear();

        if (_contractManager.ActiveContracts.Count == 0)
        {
            AddEmptyLabel(_activeSection, "No active contracts.");
            return;
        }

        foreach (Contract contract in _contractManager.ActiveContracts)
            _activeSection.Add(BuildActiveCard(contract));
    }

    private VisualElement BuildActiveCard(Contract contract)
    {
        var card = new VisualElement();
        card.AddToClassList("contract-card");
        card.AddToClassList("contract-card--active");

        var info = new VisualElement();
        info.AddToClassList("contract-card-info");

        var title = new Label(contract.ContractCategory);
        title.AddToClassList("contract-card-title");
        info.Add(title);

        var detail = new Label(
            $"Reward: £{contract.BaseRewardGBP:N0}   " +
            $"Deadline: {contract.WeeksRemaining} weeks remaining");
        detail.AddToClassList("contract-card-detail");
        info.Add(detail);

        var riskLabel = new Label($"Success chance: {contract.LockedSuccessChance:F1}% (locked)");
        riskLabel.AddToClassList("contract-card-risk");
        ApplyRiskColourClass(riskLabel, contract.LockedSuccessChance);
        info.Add(riskLabel);

        var engineers = new Label($"Engineers assigned: {contract.LockedEngineerCount}");
        engineers.AddToClassList("contract-card-detail");
        info.Add(engineers);

        card.Add(info);
        return card;
    }

    private void ShowEngineerSelection(Contract contract)
    {
        _pendingContract = contract;
        _selectedEngineerIds.Clear();

        _container.Clear();

        var view = new VisualElement();
        view.AddToClassList("contract-panel");

        var title = new Label($"Assign Engineers — {contract.ContractCategory}");
        title.AddToClassList("contract-section-header");
        view.Add(title);

        var detail = new Label(
            $"Reward: £{contract.BaseRewardGBP:N0}   Deadline: {contract.DeadlineWeeks} weeks");
        detail.AddToClassList("contract-card-detail");
        view.Add(detail);

        var engineerSection = new Label("SELECT ENGINEERS");
        engineerSection.AddToClassList("contract-section-header");
        view.Add(engineerSection);

        var availableEngineers = GetAvailableEngineers();

        if (availableEngineers.Count == 0)
        {
            var none = new Label("No unassigned Engineers on roster. Hire via EMP panel.");
            none.AddToClassList("contract-card-detail");
            view.Add(none);
        }
        else
        {
            foreach (Employee eng in availableEngineers)
                view.Add(BuildEngineerSelectRow(eng));
        }

        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.marginTop = 16f;

        var confirmBtn = new Button();
        confirmBtn.AddToClassList("contract-accept-btn");
        confirmBtn.text = "CONFIRM";
        confirmBtn.clicked += ConfirmAccept;
        buttonRow.Add(confirmBtn);

        var cancelBtn = new Button();
        cancelBtn.AddToClassList("contract-accept-btn");
        cancelBtn.style.backgroundColor = new StyleColor(new Color(0.18f, 0.25f, 0.37f));
        cancelBtn.style.marginLeft = 8f;
        cancelBtn.text = "CANCEL";
        cancelBtn.clicked += () => { _pendingContract = null; Build(); };
        buttonRow.Add(cancelBtn);

        view.Add(buttonRow);
        _container.Add(view);
    }

    private VisualElement BuildEngineerSelectRow(Employee eng)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 6f;
        row.style.paddingBottom = 6f;
        row.style.borderBottomWidth = 1f;
        row.style.borderBottomColor = new StyleColor(new Color(0.18f, 0.25f, 0.37f));

        var nameLabel = new Label($"{eng.Name} — Eff: {eng.GetModifiedStat(AegisConstants.STAT_EFFICIENCY):F0}");
        nameLabel.AddToClassList("emp-card-name");
        nameLabel.style.flexGrow = 1f;
        row.Add(nameLabel);

        var toggle = new Toggle();
        toggle.value = false;

        string capturedId = eng.EmployeeId;
        toggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue) _selectedEngineerIds.Add(capturedId);
            else _selectedEngineerIds.Remove(capturedId);
        });

        row.Add(toggle);
        return row;
    }

    private void ConfirmAccept()
    {
        if (_pendingContract == null) return;

        var selectedEngineers = new System.Collections.Generic.List<Employee>();
        foreach (Employee emp in _employeeManager.Employees)
            if (_selectedEngineerIds.Contains(emp.EmployeeId))
                selectedEngineers.Add(emp);

        if (selectedEngineers.Count == 0)
        {
            Debug.Log("[ContractPanel] No engineers selected. Select at least one.");
            return;
        }

        OnContractAcceptRequested?.Invoke(_pendingContract);
        // Note: GameHudController now passes selectedEngineers instead of auto-gathering them.
        // Store selected engineers for GameHudController to pick up.
        _lastSelectedEngineers = selectedEngineers;
        _pendingContract = null;
        Build();
    }

    // GameHudController reads this after OnContractAcceptRequested fires.
    public System.Collections.Generic.List<Employee> _lastSelectedEngineers;

    // — Private: Helpers ——————————————————————————————————

    private float GetPreviewRisk(Contract contract)
    {
        // Preview uses currently available engineers — same logic as acceptance.
        var available = GetAvailableEngineers();
        if (available.Count == 0)
            return AegisConstants.MIN_CONTRACT_CHANCE;

        return _contractManager.CalculateSuccessChance(
            available,
            requiredEngineerCount: 1,
            contractReputationTier: contract.ReputationTierRequired,
            budgetAllocated: 0f,
            contractBaseCost: contract.BaseCostGBP);
    }

    private List<Employee> GetAvailableEngineers()
    {
        var engineers = new List<Employee>();
        foreach (Employee emp in _employeeManager.Employees)
        {
            if (emp.Role == EmployeeRole.Engineer && string.IsNullOrEmpty(emp.Assignment))
                engineers.Add(emp);
        }
        return engineers;
    }

    private static void ApplyRiskColourClass(Label label, float chance)
    {
        label.RemoveFromClassList("contract-card-risk--low");
        label.RemoveFromClassList("contract-card-risk--high");

        if (chance >= 70f) label.AddToClassList("contract-card-risk--low");
        else if (chance < 40f) label.AddToClassList("contract-card-risk--high");
        // 40–69% uses default amber — no modifier class needed.
    }

    private static void AddEmptyLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("contract-card-detail");
        label.style.paddingTop = 8f;
        parent.Add(label);
    }
}
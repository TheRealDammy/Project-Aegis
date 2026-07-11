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
        acceptBtn.text = "ACCEPT";

        Contract captured = contract; // Closure capture.
        acceptBtn.clicked += () => OnContractAcceptRequested?.Invoke(captured);
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
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Builds the Contracts panel — available offers and active contract tracker.
/// Engineer assignment is handled in the EMP panel (per Game Director decision).
/// Active contracts show assigned engineer as read-only.
/// </summary>
public class ContractPanel
{
    public event Action<Contract> OnContractAcceptRequested;

    private readonly VisualElement _container;
    private readonly ContractManager _contractManager;
    private readonly EmployeeManager _employeeManager;

    private VisualElement _availableSection;
    private VisualElement _activeSection;

    public ContractPanel(VisualElement container, ContractManager contractManager,
                         EmployeeManager employeeManager)
    {
        _container = container;
        _contractManager = contractManager;
        _employeeManager = employeeManager;
    }

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("contract-panel");
        _container.Add(panel);

        AddSectionHeader(panel, "AVAILABLE CONTRACTS");
        _availableSection = new VisualElement();
        panel.Add(_availableSection);

        AddSectionHeader(panel, "ACTIVE CONTRACTS");
        _activeSection = new VisualElement();
        panel.Add(_activeSection);

        Refresh();
    }

    public void Refresh()
    {
        RefreshAvailable();
        RefreshActive();
    }

    // — Available ———————————————————————————————————————————

    private void RefreshAvailable()
    {
        _availableSection.Clear();

        if (_contractManager.AvailableContracts.Count == 0)
        {
            AddDetailLabel(_availableSection, "No contracts available.");
            return;
        }

        foreach (Contract c in _contractManager.AvailableContracts)
            _availableSection.Add(BuildAvailableCard(c));
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

        // Preview risk based on currently available engineers (informational).
        float previewChance = GetPreviewRisk(contract);
        info.Add(MakeDetailLabel(
            $"Reward: £{contract.BaseRewardGBP:N0}   " +
            $"Deadline: {contract.DeadlineWeeks}w   " +
            $"Tier: {contract.ReputationTierRequired}"));
        info.Add(MakeRiskLabel(
            $"Est. success: {previewChance:F1}%   (assign engineers in EMP panel after accepting)",
            previewChance));

        card.Add(info);

        var acceptBtn = new Button();
        acceptBtn.AddToClassList("contract-accept-btn");
        acceptBtn.text = "ACCEPT";

        Contract captured = contract;
        acceptBtn.clicked += () => OnContractAcceptRequested?.Invoke(captured);
        card.Add(acceptBtn);

        return card;
    }

    // — Active ——————————————————————————————————————————————

    private void RefreshActive()
    {
        _activeSection.Clear();

        if (_contractManager.ActiveContracts.Count == 0)
        {
            AddDetailLabel(_activeSection, "No active contracts.");
            return;
        }

        foreach (Contract c in _contractManager.ActiveContracts)
            _activeSection.Add(BuildActiveCard(c));
    }

    private VisualElement BuildActiveCard(Contract contract)
    {
        var card = new VisualElement();
        card.AddToClassList("contract-card");

        bool hasEngineer = contract.AssignedEmployeeIds.Count > 0;
        card.AddToClassList(hasEngineer ? "contract-card--active" : "contract-card--available");

        var info = new VisualElement();
        info.AddToClassList("contract-card-info");

        var title = new Label(contract.ContractCategory);
        title.AddToClassList("contract-card-title");
        info.Add(title);

        info.Add(MakeDetailLabel(
            $"Reward: £{contract.BaseRewardGBP:N0}   " +
            $"Deadline: {contract.WeeksRemaining} weeks remaining"));

        // Assigned engineer — read-only display
        string engineerName = _contractManager.GetAssignedEngineerName(contract);
        if (engineerName != null)
        {
            float currentChance = _contractManager.GetCurrentSuccessChance(contract);
            info.Add(MakeDetailLabel($"Engineer: {engineerName}"));
            info.Add(MakeRiskLabel($"Current success chance: {currentChance:F1}%", currentChance));
        }
        else
        {
            // No engineer — stall warning
            var warn = new Label("⚠ NO ENGINEER ASSIGNED — CONTRACT STALLED");
            warn.AddToClassList("contract-card-risk");
            warn.AddToClassList("contract-card-risk--high");
            info.Add(warn);
            info.Add(MakeDetailLabel("Assign an engineer in the EMP panel to resume progress."));
        }

        card.Add(info);
        return card;
    }

    // — Helpers ——————————————————————————————————————————————

    private float GetPreviewRisk(Contract contract)
    {
        var available = new List<Employee>();
        foreach (Employee emp in _employeeManager.Employees)
            if (emp.Role == EmployeeRole.Engineer && string.IsNullOrEmpty(emp.Assignment))
                available.Add(emp);

        return _contractManager.CalculateSuccessChance(
            available, Mathf.Max(1, available.Count),
            contract.ReputationTierRequired,
            0f, contract.BaseCostGBP);
    }

    private static VisualElement MakeDetailLabel(string text)
    {
        var label = new Label(text);
        label.AddToClassList("contract-card-detail");
        return label;
    }

    private static Label MakeRiskLabel(string text, float chance)
    {
        var label = new Label(text);
        label.AddToClassList("contract-card-risk");

        label.RemoveFromClassList("contract-card-risk--low");
        label.RemoveFromClassList("contract-card-risk--high");

        if (chance >= 70f) label.AddToClassList("contract-card-risk--low");
        else if (chance < 40f) label.AddToClassList("contract-card-risk--high");

        return label;
    }

    private static void AddSectionHeader(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("contract-section-header");
        parent.Add(label);
    }

    private static void AddDetailLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("contract-card-detail");
        label.style.paddingTop = 8f;
        parent.Add(label);
    }
}
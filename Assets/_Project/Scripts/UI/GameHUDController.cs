using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Owns the Game HUD UIDocument. Manages tab navigation, speed controls,
/// and delegates content to individual panel controllers.
/// </summary>
public class GameHudController : MonoBehaviour
{
    // — Serialized Fields —————————————————————————————————————
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private ResearchManager _researchManager;
    [SerializeField] private EmployeeManager _employeeManager;
    [SerializeField] private FinanceManager _financeManager;
    [SerializeField] private ContractManager _contractManager;
    [SerializeField] private ReputationManager _reputationManager;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private WorldEventManager _worldEventManager;
    [SerializeField] private RivalManager _rivalManager;
    [SerializeField] private MarketManager _marketManager;

    // — Nav Buttons ————————————————————————————————————————————
    private Button _navOverview;
    private Button _navResearch;
    private Button _navEmployees;
    private Button _navContracts;
    private Button _navMarket;
    private Button _navWorld;
    private Button[] _allNavButtons;

    // — Speed Buttons ——————————————————————————————————————————
    private Button _pauseButton;
    private Button _speed1Button;
    private Button _speed2Button;
    private Button _speed4Button;
    private Button[] _allSpeedButtons;

    // — HUD Labels —————————————————————————————————————————————
    private Label _weekLabel;
    private Label _cashLabel;
    private Label _reputationLabel;

    // — Panel Roots — all live in ContentArea simultaneously ——
    // ActivatePanel shows one, hides the rest via display style.
    private VisualElement _overviewPanelRoot;
    private VisualElement _researchPanelRoot;
    private VisualElement _employeesPanelRoot;
    private VisualElement _contractsPanelRoot;
    private VisualElement _marketPanelRoot;
    private VisualElement _worldPanelRoot;

    // — Panel Controllers ——————————————————————————————————————
    private ResearchPanel _researchPanel;
    private ContractPanel _contractPanel;
    private EmployeesPanel _employeesPanel;
    private NotificationQueue _notifications;
    private PauseOverlay _pauseOverlay;
    private SettingsPanel _settingsPanel;

    // — Internal State —————————————————————————————————————————
    private VisualElement _contentArea;

    // Add this field to track the currently active panel name
    private string _activePanelName;

    private PlayerInputActions _inputActions;
    private bool _isPaused;

    // — Unity Lifecycle ————————————————————————————————————————
    private void Awake()
    {
        if (_hudDocument == null)
        {
            Debug.LogError("[GameHudController] UIDocument not assigned in Inspector.");
            return;
        }

        // TimeManager can be assigned in Inspector or found here.
        // Inspector assignment is preferred — FindObjectOfType is a fallback.
        if (_timeManager == null)
        {
            _timeManager = FindFirstObjectByType<TimeManager>();
            if (_timeManager == null)
                Debug.LogError("[GameHudController] TimeManager not found.");
        }

        _inputActions = new PlayerInputActions();

        VisualElement root = _hudDocument.rootVisualElement;

        CacheHUDElements(root);
        BuildContentArea();
        RegisterNavCallbacks();
        RegisterSpeedCallbacks();
    }

    private void Start()
    {
        // Build() deferred from Awake — ResearchManager._nodeById is guaranteed
        // populated by now since all Awake() calls complete before any Start().
        _researchPanel?.Build();
        _contractPanel?.Build();
        _employeesPanel?.Build();

        ActivatePanel(AegisConstants.PANEL_OVERVIEW, _navOverview);
        UpdateSpeedVisualState(1f);
    }

    private void OnEnable()
    {
        _inputActions.UI.Pause.performed += OnPause;
        _inputActions.Enable();

        TimeManager.OnWeekChanged += HandleWeekChanged;
        FinanceManager.OnCashChanged += HandleCashChanged;
        ResearchManager.OnResearchCompleted += HandleResearchCompleted;
        ReputationManager.OnReputationChanged += HandleReputationChanged;
        ContractManager.OnContractCompleted += HandleContractResolved;
        ContractManager.OnContractFailed += HandleContractResolved;
        ContractManager.OnOffersUpdated += HandleOffersUpdated;
        ContractManager.OnActiveContractsUpdated += HandleActiveUpdated;
        EmployeeManager.OnEmployeeHired += HandleEmployeeHired;
        EmployeeManager.OnHiringPoolRefreshed += HandlePoolRefreshed;
        WorldEventManager.OnEventStarted += HandleWorldEventStarted;
        WorldEventManager.OnEventEnded += HandleWorldEventEnded;
        SaveManager.OnLoadAttempted += HandleLoadAttempted;
    }

    private void OnDisable()
    {
        _inputActions.UI.Pause.performed -= OnPause;
        _inputActions.Disable();

        TimeManager.OnWeekChanged -= HandleWeekChanged;
        FinanceManager.OnCashChanged -= HandleCashChanged;
        ResearchManager.OnResearchCompleted -= HandleResearchCompleted;
        ReputationManager.OnReputationChanged -= HandleReputationChanged;
        ContractManager.OnContractCompleted -= HandleContractResolved;
        ContractManager.OnContractFailed -= HandleContractResolved;
        ContractManager.OnOffersUpdated -= HandleOffersUpdated;
        ContractManager.OnActiveContractsUpdated -= HandleActiveUpdated;
        EmployeeManager.OnEmployeeHired -= HandleEmployeeHired;
        EmployeeManager.OnHiringPoolRefreshed -= HandlePoolRefreshed;
        WorldEventManager.OnEventStarted -= HandleWorldEventStarted;
        WorldEventManager.OnEventEnded -= HandleWorldEventEnded;
        SaveManager.OnLoadAttempted -= HandleLoadAttempted;
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        // ESC priority: close settings first, then close pause, then open pause.
        if (_settingsPanel.IsVisible)
        {
            _settingsPanel.Hide();
            return;
        }

        if (_isPaused)
        {
            HandleResume();
            return;
        }

        HandlePause();
    }

    // — Element Caching ————————————————————————————————————————
    private void CacheHUDElements(VisualElement root)
    {
        _weekLabel = root.Q<Label>(AegisConstants.HUD_WEEK_LABEL);
        _cashLabel = root.Q<Label>(AegisConstants.HUD_CASH_LABEL);
        _reputationLabel = root.Q<Label>(AegisConstants.HUD_REPUTATION_LABEL);

        _navOverview = root.Q<Button>(AegisConstants.HUD_NAV_OVERVIEW);
        _navResearch = root.Q<Button>(AegisConstants.HUD_NAV_RESEARCH);
        _navEmployees = root.Q<Button>(AegisConstants.HUD_NAV_EMPLOYEES);
        _navContracts = root.Q<Button>(AegisConstants.HUD_NAV_CONTRACTS);
        _navMarket = root.Q<Button>(AegisConstants.HUD_NAV_MARKET);
        _navWorld = root.Q<Button>(AegisConstants.HUD_NAV_WORLD);
        _allNavButtons = new[]
        {
            _navOverview, _navResearch, _navEmployees,
            _navContracts, _navMarket, _navWorld
        };

        _pauseButton = root.Q<Button>(AegisConstants.HUD_PAUSE_BUTTON);
        _speed1Button = root.Q<Button>(AegisConstants.HUD_SPEED1_BUTTON);
        _speed2Button = root.Q<Button>(AegisConstants.HUD_SPEED2_BUTTON);
        _speed4Button = root.Q<Button>(AegisConstants.HUD_SPEED4_BUTTON);
        _allSpeedButtons = new[]
        {
            _pauseButton, _speed1Button, _speed2Button, _speed4Button
        };

        _contentArea = root.Q<VisualElement>("ContentArea");

        // Validate everything. Q<>() returns null silently — catch it here.
        if (_weekLabel == null) Debug.LogError("[GameHudController] WeekLabel not found in UXML.");
        if (_cashLabel == null) Debug.LogError("[GameHudController] CashLabel not found in UXML.");
        if (_contentArea == null) Debug.LogError("[GameHudController] ContentArea not found in UXML. " +
                                                  "Tab switching will not work.");
        if (_reputationLabel == null)
            Debug.LogError("[GameHudController] ReputationLabel not found in UXML.");

        foreach (var btn in _allNavButtons)
            if (btn == null) Debug.LogError("[GameHudController] Nav button returned null from Q<>(). " +
                                             "Check UXML name attributes match AegisConstants.");

        foreach (var btn in _allSpeedButtons)
            if (btn == null) Debug.LogError("[GameHudController] Speed button returned null from Q<>().");
    }

    // — Content Area Setup —————————————————————————————————————

    /// <summary>
    /// Clears any UXML-defined children from ContentArea (removes the old
    /// ActivePanelLabel placeholder) and builds one root VE per panel.
    /// All roots start hidden — ActivatePanel reveals the correct one.
    /// </summary>
    private void BuildContentArea()
    {
        if (_contentArea == null) return;

        _contentArea.Clear();

        _overviewPanelRoot = MakePanelRoot();
        _researchPanelRoot = MakePanelRoot();
        _employeesPanelRoot = MakePanelRoot();
        _contractsPanelRoot = MakePanelRoot();
        _marketPanelRoot = MakePanelRoot();
        _worldPanelRoot = MakePanelRoot();

        // Stubs for unimplemented panels.
        AddStubLabel(_overviewPanelRoot, "OVERVIEW");
        AddStubLabel(_employeesPanelRoot, "EMPLOYEES");
        AddStubLabel(_contractsPanelRoot, "CONTRACTS");
        AddStubLabel(_marketPanelRoot, "MARKET");
        AddStubLabel(_worldPanelRoot, "WORLD");

        // Research panel constructed here but NOT built yet.
        // Build() is deferred to Start() so ResearchManager.Awake()
        // has time to populate NodeSOLookup before we query it.
        if (_researchManager != null && _employeeManager != null)
        {
            _researchPanel = new ResearchPanel(
                _researchPanelRoot, _researchManager, _employeeManager);
            _researchPanel.OnAssignResearcherRequested += HandleAssignResearcher;
            _researchPanel.OnCancelResearchRequested += HandleCancelResearch;
        }
        else
        {
            AddStubLabel(_researchPanelRoot, "RESEARCH — assign managers in Inspector");
            Debug.LogWarning("[GameHudController] ResearchManager or EmployeeManager not assigned.");
        }

        // Pause overlay and settings — overlays added to root, not ContentArea
        VisualElement root = _hudDocument.rootVisualElement;
        _pauseOverlay = new PauseOverlay(root);
        _settingsPanel = new SettingsPanel(root, SettingsManager.Instance, _saveManager);

        _pauseOverlay.OnResume += HandleResume;
        _pauseOverlay.OnOpenSettings += HandleOpenSettings;

        if (_contractManager != null && _employeeManager != null)
        {
            _contractPanel = new ContractPanel(
                _contractsPanelRoot, _contractManager, _employeeManager);
            _contractPanel.OnContractAcceptRequested += HandleContractAccept;
        }

        if (_contractManager != null && _employeeManager != null)
        {
            _employeesPanel = new EmployeesPanel(
                _employeesPanelRoot, _employeeManager, _contractManager);
            _employeesPanel.OnHireRequested += HandleHireRequested;
            _employeesPanel.OnAssignToContractRequested += HandleAssignToContract;
        }

        // Notification system — attached to root so banners float above all panels.
        _notifications = new NotificationQueue(
            _hudDocument.rootVisualElement);
    }

    private VisualElement MakePanelRoot()
    {
        var ve = new VisualElement();
        ve.style.flexGrow = 1f;
        ve.style.flexShrink = 1f;
        ve.style.display = DisplayStyle.None;
        _contentArea.Add(ve);
        return ve;
    }

    private void AddStubLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("panel-placeholder-label");
        parent.Add(label);
    }

    // — Nav Callbacks ——————————————————————————————————————————
    private void RegisterNavCallbacks()
    {
        if (_navOverview == null || _navResearch == null) return;

        _navOverview.clicked += () => ActivatePanel(AegisConstants.PANEL_OVERVIEW, _navOverview);
        _navResearch.clicked += () => ActivatePanel(AegisConstants.PANEL_RESEARCH, _navResearch);
        _navEmployees.clicked += () => ActivatePanel(AegisConstants.PANEL_EMPLOYEES, _navEmployees);
        _navContracts.clicked += () => ActivatePanel(AegisConstants.PANEL_CONTRACTS, _navContracts);
        _navMarket.clicked += () => ActivatePanel(AegisConstants.PANEL_MARKET, _navMarket);
        _navWorld.clicked += () => ActivatePanel(AegisConstants.PANEL_WORLD, _navWorld);
    }

    private void ActivatePanel(string panelName, Button sourceButton)
    {
        _activePanelName = panelName;

        // Nav button state.
        foreach (var btn in _allNavButtons)
        {
            if (btn != null)
                btn.RemoveFromClassList("nav-btn--active");
        }

        if (sourceButton != null)
            sourceButton.AddToClassList("nav-btn--active");

        // Hide every panel, then show the one that matches.
        SetPanelDisplay(_overviewPanelRoot, panelName == AegisConstants.PANEL_OVERVIEW);
        SetPanelDisplay(_researchPanelRoot, panelName == AegisConstants.PANEL_RESEARCH);
        SetPanelDisplay(_employeesPanelRoot, panelName == AegisConstants.PANEL_EMPLOYEES);
        SetPanelDisplay(_contractsPanelRoot, panelName == AegisConstants.PANEL_CONTRACTS);
        SetPanelDisplay(_marketPanelRoot, panelName == AegisConstants.PANEL_MARKET);
        SetPanelDisplay(_worldPanelRoot, panelName == AegisConstants.PANEL_WORLD);

        // Refresh content when it becomes visible.
        if (panelName == AegisConstants.PANEL_RESEARCH) _researchPanel?.Refresh();
        if (panelName == AegisConstants.PANEL_CONTRACTS) _contractPanel?.Refresh();
        if (panelName == AegisConstants.PANEL_EMPLOYEES) _employeesPanel?.Refresh();

        Debug.Log($"[GameHud] Panel active: {panelName}");
    }

    private static void SetPanelDisplay(VisualElement panel, bool visible)
    {
        if (panel != null)
            panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // — Speed Callbacks ————————————————————————————————————————
    private void RegisterSpeedCallbacks()
    {
        if (_pauseButton == null) return;

        _pauseButton.clicked += () => SetSimSpeed(0f);
        _speed1Button.clicked += () => SetSimSpeed(1f);
        _speed2Button.clicked += () => SetSimSpeed(2f);
        _speed4Button.clicked += () => SetSimSpeed(4f);
    }

    private void SetSimSpeed(float speed)
    {
        if (_timeManager == null) return;
        _timeManager.SetSpeed(speed);
        UpdateSpeedVisualState(speed);
    }

    private void UpdateSpeedVisualState(float activeSpeed)
    {
        foreach (var btn in _allSpeedButtons)
            if (btn != null) btn.RemoveFromClassList("speed-btn--active");

        Button active = activeSpeed switch
        {
            0f => _pauseButton,
            1f => _speed1Button,
            2f => _speed2Button,
            4f => _speed4Button,
            _ => _speed1Button
        };

        active?.AddToClassList("speed-btn--active");
    }

    // — Event Handlers —————————————————————————————————————————
    private void HandleWeekChanged(int newWeek)
    {
        if (_weekLabel != null)
            _weekLabel.text = $"WEEK {newWeek}";
    }

    private void HandleCashChanged(float newCash)
    {
        if (_cashLabel != null)
            _cashLabel.text = $"£{newCash:N0}";
    }

    private void HandleResearchCompleted(ResearchNodeSO node)
    {
        // Refresh research panel if it's currently visible.
        _researchPanel?.Refresh();
    }

    private void HandleAssignResearcher(string nodeId)
    {
        if (_employeeManager == null || _researchManager == null) return;

        Employee researcher = null;
        foreach (var emp in _employeeManager.Employees)
        {
            if (emp.Role == EmployeeRole.Researcher && string.IsNullOrEmpty(emp.Assignment))
            {
                researcher = emp;
                break;
            }
        }

        if (researcher == null)
        {
            Debug.Log("[GameHudController] No unassigned Researchers on roster.");
            return;
        }

        _researchManager.AssignResearcher(nodeId, researcher);
        _researchPanel?.Refresh();
    }

    private void HandleCancelResearch(string nodeId)
    {
        _researchManager?.CancelResearch(nodeId);
        _researchPanel?.Refresh();
    }

    private void HandleReputationChanged(float score)
    {
        // ReputationManager.CurrentTier is set before this event fires.
        if (_reputationLabel != null && _reputationManager != null)
            _reputationLabel.text = ReputationManager.GetTierName(_reputationManager.CurrentTier);
    }

    private void HandleContractResolved(Contract contract)
    {
        _contractPanel?.Refresh();
    }

    private void HandleOffersUpdated(IReadOnlyList<Contract> offers)
    {
        if (_activePanelName == AegisConstants.PANEL_CONTRACTS)
            _contractPanel?.Refresh();
    }

    private void HandleActiveUpdated(IReadOnlyList<Contract> active)
    {
        if (_activePanelName == AegisConstants.PANEL_CONTRACTS)
            _contractPanel?.Refresh();
    }

    private void HandleContractAccept(Contract contract)
    {
        _contractManager?.AcceptContract(contract);
        _contractPanel?.Refresh();

        // Prompt player to assign an engineer
        _notifications?.Show(
            "Contract Accepted",
            $"{contract.ContractCategory} — assign an engineer in the EMP panel.",
            NotificationQueue.Type.Warning);
    }

    private void HandleWorldEventStarted(WorldEventSO worldEvent)
    {
        _notifications?.Show(
            worldEvent.EventName,
            worldEvent.Description,
            NotificationQueue.Type.Warning);
    }

    private void HandleWorldEventEnded(WorldEventSO worldEvent)
    {
        _notifications?.Show(
            $"{worldEvent.EventName} — Ended",
            "Market conditions returning to baseline.",
            NotificationQueue.Type.Success);
    }

    private void HandlePause()
    {
        _isPaused = true;
        _timeManager?.SetSpeed(0f);
        UpdateSpeedVisualState(0f);
        _pauseOverlay.Show();
    }

    private void HandleResume()
    {
        _isPaused = false;
        _pauseOverlay.Hide();
        _settingsPanel.Hide();
        _timeManager?.SetSpeed(1f);
        UpdateSpeedVisualState(1f);
    }

    private void HandleOpenSettings()
    {
        _settingsPanel.Show();
    }

    private void HandleLoadAttempted(SaveLoadResult result, string message)
    {
        NotificationQueue.Type type = result == SaveLoadResult.Success
            ? NotificationQueue.Type.Success
            : NotificationQueue.Type.Failure;

        _notifications?.Show(
            result == SaveLoadResult.VersionMismatch ? "Save File Incompatible"
          : result == SaveLoadResult.Success ? "Game Loaded"
          : result == SaveLoadResult.FileNotFound ? "No Save Found"
          : "Load Failed",
            message,
            type);
    }

    // ——— Hiring ————————————————————————————————————————————————

    private void HandleHireRequested(HiringCandidate candidate)
    {
        bool hired = _employeeManager.HireCandidate(candidate);
        if (hired)
        {
            _employeesPanel?.Refresh();
            _contractPanel?.Refresh();
        }
    }

    private void HandleEmployeeHired(Employee emp)
    {
        _employeesPanel?.Refresh();
    }

    private void HandlePoolRefreshed(IReadOnlyList<HiringCandidate> pool)
    {
        if (_activePanelName == AegisConstants.PANEL_EMPLOYEES)
            _employeesPanel?.Refresh();
    }

    // ——— Engineer Assignment ————————————————————————————————————

    private void HandleAssignToContract(Employee engineer, string contractId)
    {
        if (_contractManager == null) return;

        bool success = _contractManager.AssignEngineer(contractId, engineer);
        if (success)
        {
            _employeesPanel?.Refresh();
            _contractPanel?.Refresh();
            _notifications?.Show(
                "Engineer Assigned",
                $"{engineer.Name} → {contractId}",
                NotificationQueue.Type.Success);
        }
        else
        {
            _notifications?.Show(
                "Assignment Failed",
                $"{engineer.Name} could not be assigned to {contractId}.",
                NotificationQueue.Type.Failure);
        }
    }

    // ——— Contract Status ————————————————————————————————————————

    private void HandleContractUnstaffed(Contract contract)
    {
        _notifications?.Show(
            "Contract Stalled",
            $"{contract.ContractId} ({contract.ContractCategory}) — " +
            $"no engineer assigned. Go to EMP panel.",
            NotificationQueue.Type.Warning);

        if (_activePanelName == AegisConstants.PANEL_CONTRACTS)
            _contractPanel?.Refresh();
    }

    private void HandleActiveContractsUpdated(IReadOnlyList<Contract> active)
    {
        if (_activePanelName == AegisConstants.PANEL_CONTRACTS)
            _contractPanel?.Refresh();
    }
}
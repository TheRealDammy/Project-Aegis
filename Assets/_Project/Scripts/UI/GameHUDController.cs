using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Owns the Game HUD UIDocument. Manages tab navigation and panel-switching.
/// At M0: swaps a placeholder label. At M1+: shows/hides real panel VisualElements.
/// </summary>
public class GameHudController : MonoBehaviour
{
    // — Serialized Fields —————————————————————————————————————
    [SerializeField] private UIDocument _hudDocument;

    // — Private Fields ————————————————————————————————————————
    private VisualElement _root;

    private Label _activePanelLabel;

    private Button _navOverview;
    private Button _navResearch;
    private Button _navEmployees;
    private Button _navContracts;
    private Button _navMarket;
    private Button _navWorld;
    private Label _weekLabel;

    // Cached array for batch operations (active state toggle).
    // Order must match the UXML top-to-bottom order.
    private Button[] _allNavButtons;

    // — Unity Lifecycle ———————————————————————————————————————
    private void Awake()
    {
        if (_hudDocument == null)
        {
            Debug.LogError("[GameHudController] UIDocument is not assigned in the Inspector.");
            return;
        }

        _root = _hudDocument.rootVisualElement;

        CacheElements();
        RegisterNavCallbacks();
    }

    private void Start()
    {
        // Default to Overview on scene load.
        // Called in Start (not Awake) so all Awake caching completes first.
        ActivatePanel(AegisConstants.PANEL_OVERVIEW, _navOverview);
    }

    private void OnEnable()
    {
        TimeManager.OnWeekChanged += HandleWeekChanged;
    }

    private void OnDisable()
    {
        TimeManager.OnWeekChanged -= HandleWeekChanged;
    }

    // — Private Methods ———————————————————————————————————————

    private void CacheElements()
    {
        _activePanelLabel = _root.Q<Label>(AegisConstants.HUD_ACTIVE_PANEL_LABEL);

        _navOverview = _root.Q<Button>(AegisConstants.HUD_NAV_OVERVIEW);
        _navResearch = _root.Q<Button>(AegisConstants.HUD_NAV_RESEARCH);
        _navEmployees = _root.Q<Button>(AegisConstants.HUD_NAV_EMPLOYEES);
        _navContracts = _root.Q<Button>(AegisConstants.HUD_NAV_CONTRACTS);
        _navMarket = _root.Q<Button>(AegisConstants.HUD_NAV_MARKET);
        _navWorld = _root.Q<Button>(AegisConstants.HUD_NAV_WORLD);
        _weekLabel = _root.Q<Label>(AegisConstants.HUD_WEEK_LABEL);

        if (_weekLabel == null)
            Debug.LogError("[GameHudController] WeekLabel not found in UXML.");

        _allNavButtons = new[]
        {
            _navOverview, _navResearch, _navEmployees,
            _navContracts, _navMarket, _navWorld
        };

        // Validate — Q<>() returns null silently. Catch it here, not at click time.
        if (_activePanelLabel == null)
            Debug.LogError("[GameHudController] ActivePanelLabel not found. Check UXML name attribute.");

        foreach (Button btn in _allNavButtons)
        {
            if (btn == null)
                Debug.LogError("[GameHudController] A nav button returned null from Q<>(). " +
                               "Verify UXML name attributes match AegisConstants.");
        }
    }

    private void RegisterNavCallbacks()
    {
        // Each lambda captures its specific panel name and button reference.
        // Not a loop variable capture — each closure is distinct.
        _navOverview.clicked += () => ActivatePanel(AegisConstants.PANEL_OVERVIEW, _navOverview);
        _navResearch.clicked += () => ActivatePanel(AegisConstants.PANEL_RESEARCH, _navResearch);
        _navEmployees.clicked += () => ActivatePanel(AegisConstants.PANEL_EMPLOYEES, _navEmployees);
        _navContracts.clicked += () => ActivatePanel(AegisConstants.PANEL_CONTRACTS, _navContracts);
        _navMarket.clicked += () => ActivatePanel(AegisConstants.PANEL_MARKET, _navMarket);
        _navWorld.clicked += () => ActivatePanel(AegisConstants.PANEL_WORLD, _navWorld);
    }

    private void ActivatePanel(string panelName, Button sourceButton)
    {
        // Update placeholder label — replaced by real panel show/hide logic at M1.
        if (_activePanelLabel != null)
            _activePanelLabel.text = panelName.ToUpper();

        // Toggle active class on nav buttons.
        // Remove from all, then add to the clicked one.
        foreach (Button btn in _allNavButtons)
            btn.RemoveFromClassList("nav-btn--active");

        sourceButton.AddToClassList("nav-btn--active");

        Debug.Log($"[GameHud] Panel activated: {panelName}");
    }

    private void HandleWeekChanged(int newWeek)
    {
        if (_weekLabel != null)
            _weekLabel.text = $"WEEK {newWeek}";
    }
}
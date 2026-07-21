using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Five-beat sequential tutorial state machine. Records as DD-15.
/// Implemented as a MonoBehaviour so it can subscribe to simulation events.
/// Reads/writes TutorialComplete in GameSaveData. SaveVersion 3.
/// </summary>
public class TutorialController : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    // None — TutorialController reads from the simulation, doesn't emit to it.

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private UIDocument _hudDocument;

    // — Beat State —————————————————————————————————————————
    private enum Beat
    {
        Intro,           // Showing initial prompt, waiting for unpause
        WaitForContract, // Speed > 0 observed, waiting for contract accept
        WaitForEngineer, // Contract accepted, waiting for engineer hire
        WaitForAssign,   // Engineer hired, waiting for assignment
        WaitForDelivery, // Engineer assigned, waiting for contract complete
        Complete         // Tutorial done — strip dismissed
    }

    private Beat _currentBeat = Beat.Intro;

    // — UI References ——————————————————————————————————————
    private VisualElement _strip;
    private Label _promptLabel;
    private Button _skipButton;

    // — Prompt text per beat ———————————————————————————————
    private static readonly string[] _prompts =
    {
        /* Intro          */ "Welcome to Aegis Systems. Press ▶ in the top bar to start the simulation.",
        /* WaitForContract*/ "Simulation running. Open the CON tab and accept an available contract.",
        /* WaitForEngineer*/ "Contract accepted. Open the EMP tab and hire an Engineer from the pool.",
        /* WaitForAssign  */ "Engineer hired. In the EMP tab, assign them to your active contract.",
        /* WaitForDelivery*/ "Engineer assigned. Your contract is progressing. Watch the CON tab for delivery.",
        /* Complete       */ "" // Never shown — strip is dismissed on reaching this beat.
    };

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        if (_hudDocument == null)
        {
            Debug.LogWarning("[TutorialController] UIDocument not assigned — tutorial disabled.");
            return;
        }

        CacheElements();
        RegisterSkipButton();
        // Note: IsComplete loaded from save data via LoadFromSaveData before Start().
        // If already complete, strip is still hidden (DisplayStyle.None in UXML default).
    }

    private void OnEnable()
    {
        TimeManager.OnSpeedChanged += HandleSpeedChanged;
        ContractManager.OnContractAccepted += HandleContractAccepted;
        EmployeeManager.OnEmployeeHired += HandleEmployeeHired;
        ContractManager.OnEngineerAssigned += HandleEngineerAssigned;
        ContractManager.OnContractCompleted += HandleContractCompleted;
    }

    private void OnDisable()
    {
        TimeManager.OnSpeedChanged -= HandleSpeedChanged;
        ContractManager.OnContractAccepted -= HandleContractAccepted;
        EmployeeManager.OnEmployeeHired -= HandleEmployeeHired;
        ContractManager.OnEngineerAssigned -= HandleEngineerAssigned;
        ContractManager.OnContractCompleted -= HandleContractCompleted;
    }

    // — Public: Save / Load ————————————————————————————————

    /// <summary>True if the tutorial has been completed or skipped this session or a prior one.</summary>
    public bool IsComplete => _currentBeat == Beat.Complete;

    public void PopulateSaveData(GameSaveData data)
    {
        data.TutorialComplete = IsComplete;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        if (!data.TutorialComplete) return;

        // Already done — skip to Complete without showing the strip.
        _currentBeat = Beat.Complete;
        Debug.Log("[TutorialController] Tutorial already complete — skipping.");
    }

    /// <summary>
    /// Called by GameHudController after all Awake caches are done.
    /// Displays the initial tutorial prompt if not already complete.
    /// </summary>
    public void Initialise()
    {
        if (IsComplete) return;
        ShowStrip();
        UpdatePrompt();
    }

    // — Private: Element Caching ———————————————————————————

    private void CacheElements()
    {
        VisualElement root = _hudDocument.rootVisualElement;
        _strip = root.Q<VisualElement>(AegisConstants.HUD_TUTORIAL_STRIP);
        _promptLabel = root.Q<Label>(AegisConstants.HUD_TUTORIAL_PROMPT);
        _skipButton = root.Q<Button>(AegisConstants.HUD_TUTORIAL_SKIP);

        if (_strip == null)
            Debug.LogError("[TutorialController] TutorialStrip not found in UXML. " +
                           "Check GameHud.uxml for the element name.");
    }

    private void RegisterSkipButton()
    {
        if (_skipButton != null)
            _skipButton.clicked += Complete;
    }

    // — Private: Beat Handlers ————————————————————————————

    private void HandleSpeedChanged(float speed)
    {
        if (_currentBeat != Beat.Intro) return;
        if (speed > 0f) AdvanceBeat();
    }

    private void HandleContractAccepted(Contract _)
    {
        if (_currentBeat != Beat.WaitForContract) return;
        AdvanceBeat();
    }

    private void HandleEmployeeHired(Employee emp)
    {
        if (_currentBeat != Beat.WaitForEngineer) return;
        if (emp.Role != EmployeeRole.Engineer) return;
        AdvanceBeat();
    }

    private void HandleEngineerAssigned(Contract _, Employee __)
    {
        if (_currentBeat != Beat.WaitForAssign) return;
        AdvanceBeat();
    }

    private void HandleContractCompleted(Contract _)
    {
        if (_currentBeat != Beat.WaitForDelivery) return;
        AdvanceBeat();
    }

    // — Private: State Machine ————————————————————————————

    private void AdvanceBeat()
    {
        _currentBeat = (Beat)((int)_currentBeat + 1);

        if (_currentBeat == Beat.Complete)
        {
            Complete();
            return;
        }

        UpdatePrompt();
        Debug.Log($"[TutorialController] Beat advanced to {_currentBeat}.");
    }

    private void UpdatePrompt()
    {
        if (_promptLabel == null) return;
        int index = (int)_currentBeat;
        if (index < _prompts.Length)
            _promptLabel.text = _prompts[index];
    }

    private void Complete()
    {
        _currentBeat = Beat.Complete;
        HideStrip();
        Debug.Log("[TutorialController] Tutorial complete.");
    }

    private void ShowStrip()
    {
        if (_strip != null)
            _strip.style.display = DisplayStyle.Flex;
    }

    private void HideStrip()
    {
        if (_strip != null)
            _strip.style.display = DisplayStyle.None;
    }
}
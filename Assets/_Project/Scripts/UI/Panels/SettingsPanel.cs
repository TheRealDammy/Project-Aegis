using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UITK modal settings panel. Covers audio, display, and save slot management.
/// Plain C# class — instantiated by GameHudController and PauseOverlay.
/// Added to the document root so it appears above all game panels.
/// </summary>
public class SettingsPanel
{
    private readonly VisualElement _documentRoot;
    private readonly SettingsManager _settings;
    private readonly SaveManager _saveManager;

    private VisualElement _backdrop;
    private VisualElement _audioContent;
    private VisualElement _displayContent;
    private VisualElement _saveContent;

    private const string TAB_AUDIO = "AUDIO";
    private const string TAB_DISPLAY = "DISPLAY";
    private const string TAB_SAVE = "SAVE DATA";

    private string _activeTab = TAB_AUDIO;
    private List<Button> _tabButtons = new();

    public bool IsVisible => _backdrop != null && _backdrop.parent != null;

    // — Constructor —————————————————————————————————————————
    public SettingsPanel(VisualElement documentRoot,
                         SettingsManager settingsManager,
                         SaveManager saveManager)
    {
        _documentRoot = documentRoot;
        _settings = settingsManager;
        _saveManager = saveManager;
    }

    // — Public ——————————————————————————————————————————————

    public void Show()
    {
        if (IsVisible) return;
        BuildModal();
        _documentRoot.Add(_backdrop);
    }

    public void Hide()
    {
        if (!IsVisible) return;
        _documentRoot.Remove(_backdrop);
    }

    // — Build ————————————————————————————————————————————————

    private void BuildModal()
    {
        // Full-screen backdrop
        _backdrop = new VisualElement();
        _backdrop.AddToClassList("modal-backdrop");

        // Modal panel
        var panel = new VisualElement();
        panel.AddToClassList("modal-panel");
        panel.AddToClassList("settings-panel");
        _backdrop.Add(panel);

        // Header
        panel.Add(BuildHeader());

        // Tab bar
        panel.Add(BuildTabBar());

        // Content area — three sections, shown one at a time
        _audioContent = BuildAudioContent();
        _displayContent = BuildDisplayContent();
        _saveContent = BuildSaveContent();

        panel.Add(_audioContent);
        panel.Add(_displayContent);
        panel.Add(_saveContent);

        ShowTab(TAB_AUDIO);
    }

    private VisualElement BuildHeader()
    {
        var header = new VisualElement();
        header.AddToClassList("modal-header");

        var title = new Label("SETTINGS");
        title.AddToClassList("modal-title");
        header.Add(title);

        var closeBtn = new Button(Hide);
        closeBtn.AddToClassList("modal-close-btn");
        closeBtn.text = "✕";
        header.Add(closeBtn);

        return header;
    }

    private VisualElement BuildTabBar()
    {
        _tabButtons.Clear();
        var tabBar = new VisualElement();
        tabBar.AddToClassList("settings-tab-bar");

        foreach (string tab in new[] { TAB_AUDIO, TAB_DISPLAY, TAB_SAVE })
        {
            string capturedTab = tab;
            var btn = new Button(() => ShowTab(capturedTab));
            btn.AddToClassList("settings-tab");
            btn.text = tab;
            _tabButtons.Add(btn);
            tabBar.Add(btn);
        }

        return tabBar;
    }

    private void ShowTab(string tab)
    {
        _activeTab = tab;

        _audioContent.style.display = tab == TAB_AUDIO ? DisplayStyle.Flex : DisplayStyle.None;
        _displayContent.style.display = tab == TAB_DISPLAY ? DisplayStyle.Flex : DisplayStyle.None;
        _saveContent.style.display = tab == TAB_SAVE ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (Button btn in _tabButtons)
            btn.EnableInClassList("settings-tab--active", btn.text == tab);

        // Rebuild save content on each open — slot states may have changed.
        if (tab == TAB_SAVE) RebuildSaveContent();
    }

    // — Audio Content ————————————————————————————————————————

    private VisualElement BuildAudioContent()
    {
        var content = new VisualElement();
        content.AddToClassList("settings-content");

        content.Add(BuildVolumeRow("MASTER", _settings.MasterVolume,
            v => _settings.SetMasterVolume(v)));
        content.Add(BuildVolumeRow("SFX", _settings.SFXVolume,
            v => _settings.SetSFXVolume(v)));
        content.Add(BuildVolumeRow("MUSIC", _settings.MusicVolume,
            v => _settings.SetMusicVolume(v)));

        var note = new Label(
            "Audio system is not yet implemented. " +
            "Volume settings are saved and will apply when audio is added.");
        note.AddToClassList("cell-text-secondary");
        note.style.marginTop = 16f;
        note.style.whiteSpace = WhiteSpace.Normal;
        content.Add(note);

        return content;
    }

    private VisualElement BuildVolumeRow(string labelText, float currentValue,
                                          System.Action<float> onChange)
    {
        var row = new VisualElement();
        row.AddToClassList("setting-row");

        var label = new Label(labelText);
        label.AddToClassList("setting-label");
        row.Add(label);

        var slider = new Slider(0f, 1f) { value = currentValue };
        slider.AddToClassList("setting-control");
        slider.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        row.Add(slider);

        // Value readout
        var readout = new Label($"{currentValue * 100f:F0}%");
        readout.AddToClassList("cell-text-data");
        readout.style.width = 40f;
        readout.style.unityTextAlign = TextAnchor.MiddleRight;
        slider.RegisterValueChangedCallback(
            evt => readout.text = $"{evt.newValue * 100f:F0}%");
        row.Add(readout);

        return row;
    }

    // — Display Content ——————————————————————————————————————

    private VisualElement BuildDisplayContent()
    {
        var content = new VisualElement();
        content.AddToClassList("settings-content");

        // Fullscreen toggle
        var fsRow = new VisualElement();
        fsRow.AddToClassList("setting-row");

        var fsLabel = new Label("FULLSCREEN");
        fsLabel.AddToClassList("setting-label");

        var fsToggle = new Toggle { value = Screen.fullScreen };
        fsToggle.AddToClassList("setting-control");
        fsToggle.RegisterValueChangedCallback(
            evt => _settings.SetFullscreen(evt.newValue));

        fsRow.Add(fsLabel);
        fsRow.Add(fsToggle);
        content.Add(fsRow);

        // Resolution dropdown
        Resolution[] resolutions = _settings.GetUniqueResolutions();
        var choices = new List<string>();
        foreach (Resolution r in resolutions)
            choices.Add($"{r.width} × {r.height}");

        int currentIndex = Mathf.Clamp(_settings.ResolutionIndex, 0, choices.Count - 1);

        var resRow = new VisualElement();
        resRow.AddToClassList("setting-row");

        var resLabel = new Label("RESOLUTION");
        resLabel.AddToClassList("setting-label");

        var resDropdown = new DropdownField(choices, currentIndex);
        resDropdown.AddToClassList("setting-control");
        resDropdown.RegisterValueChangedCallback(
            _ => _settings.SetResolution(resDropdown.index));

        resRow.Add(resLabel);
        resRow.Add(resDropdown);
        content.Add(resRow);

        return content;
    }

    // — Save Data Content ————————————————————————————————————

    private VisualElement _saveContentContainer;

    private VisualElement BuildSaveContent()
    {
        var content = new VisualElement();
        content.AddToClassList("settings-content");

        _saveContentContainer = new VisualElement();
        content.Add(_saveContentContainer);

        RebuildSaveContent();
        return content;
    }

    private void RebuildSaveContent()
    {
        if (_saveContentContainer == null) return;
        _saveContentContainer.Clear();

        if (_saveManager == null)
        {
            AddSaveNote(_saveContentContainer, "Save manager not available.");
            return;
        }

        List<SaveSlotInfo> slots = _saveManager.GetAllSlotInfo();

        foreach (SaveSlotInfo slot in slots)
        {
            var row = BuildSlotRow(slot, slots.IndexOf(slot));
            _saveContentContainer.Add(row);
        }
    }

    private VisualElement BuildSlotRow(SaveSlotInfo slot, int rowIndex)
    {
        var row = new VisualElement();
        row.AddToClassList("slot-row");
        row.AddToClassList(rowIndex % 2 == 0 ? "slot-row--even" : "slot-row--odd");

        // Slot name
        var nameLabel = new Label(slot.SlotLabel);
        nameLabel.AddToClassList("slot-name");
        row.Add(nameLabel);

        if (!slot.Exists)
        {
            var emptyLabel = new Label("Empty");
            emptyLabel.AddToClassList("cell-text-secondary");
            emptyLabel.style.flexGrow = 1f;
            row.Add(emptyLabel);
        }
        else if (slot.IsCorrupted)
        {
            var corruptLabel = new Label("CORRUPTED — cannot load");
            corruptLabel.AddToClassList("slot-status-corrupted");
            corruptLabel.style.flexGrow = 1f;
            row.Add(corruptLabel);
        }
        else
        {
            // Week
            var weekLabel = new Label($"Week {slot.CurrentWeek}");
            weekLabel.AddToClassList("slot-week");
            row.Add(weekLabel);

            // Timestamp
            var timeLabel = new Label(slot.FormattedTimestamp);
            timeLabel.AddToClassList("slot-time");
            row.Add(timeLabel);

            // Compatibility warning
            if (!slot.IsCompatible)
            {
                var compatLabel = new Label($"v{slot.SaveVersion} — incompatible");
                compatLabel.AddToClassList("slot-status-incompatible");
                row.Add(compatLabel);
            }
        }

        // Delete button
        var deleteBtn = new Button();
        deleteBtn.AddToClassList("slot-delete-btn");
        deleteBtn.text = "DELETE";
        deleteBtn.SetEnabled(slot.Exists);

        int capturedSlot = slot.Slot;
        deleteBtn.clicked += () =>
        {
            _saveManager.DeleteSlot(capturedSlot);
            RebuildSaveContent(); // Refresh after delete.
        };

        row.Add(deleteBtn);
        return row;
    }

    private static void AddSaveNote(VisualElement parent, string message)
    {
        var label = new Label(message);
        label.AddToClassList("cell-text-secondary");
        parent.Add(label);
    }
}
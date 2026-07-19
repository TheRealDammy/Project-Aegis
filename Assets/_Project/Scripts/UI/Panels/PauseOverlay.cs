using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ESC-triggered pause overlay. Shows Resume, Settings, Quit buttons.
/// Plain C# class owned by GameHudController.
/// </summary>
public class PauseOverlay
{
    public event Action OnResume;
    public event Action OnOpenSettings;

    private readonly VisualElement _documentRoot;
    private VisualElement _backdrop;

    public bool IsVisible => _backdrop != null && _backdrop.parent != null;

    public PauseOverlay(VisualElement documentRoot)
    {
        _documentRoot = documentRoot;
    }

    public void Show()
    {
        if (IsVisible) return;
        BuildOverlay();
        _documentRoot.Add(_backdrop);
    }

    public void Hide()
    {
        if (!IsVisible) return;
        _documentRoot.Remove(_backdrop);
    }

    private void BuildOverlay()
    {
        _backdrop = new VisualElement();
        _backdrop.AddToClassList("modal-backdrop");

        var panel = new VisualElement();
        panel.AddToClassList("modal-panel");
        panel.AddToClassList("pause-panel");
        _backdrop.Add(panel);

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");

        var title = new Label("PAUSED");
        title.AddToClassList("modal-title");
        header.Add(title);
        panel.Add(header);

        // Buttons
        panel.Add(BuildPauseButton("RESUME", () => OnResume?.Invoke()));
        panel.Add(BuildPauseButton("SETTINGS", () => OnOpenSettings?.Invoke()));
        panel.Add(BuildPauseButton("QUIT TO DESKTOP",
            () => Application.Quit(),
            danger: true));
    }

    private static VisualElement BuildPauseButton(string label,
                                                   Action onClick,
                                                   bool danger = false)
    {
        var btn = new Button(onClick);
        btn.AddToClassList("pause-menu-btn");
        if (danger) btn.AddToClassList("pause-menu-btn--danger");
        btn.text = label;
        return btn;
    }
}
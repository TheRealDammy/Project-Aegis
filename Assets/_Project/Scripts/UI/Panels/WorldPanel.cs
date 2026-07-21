using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// World panel — displays active world events and their market effects.
/// QA-009 PARTIAL CLOSURE: event data renders correctly.
/// DEFERRED: 2D political world map (requires art assets not yet created).
/// </summary>
public class WorldPanel
{
    private readonly VisualElement _container;
    private readonly WorldEventManager _worldEventManager;

    public WorldPanel(VisualElement container, WorldEventManager worldEventManager)
    {
        _container = container;
        _worldEventManager = worldEventManager;
    }

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("world-panel");
        _container.Add(panel);

        BuildEventSection(panel);
        BuildDeferredNote(panel);

        Refresh();
    }

    private VisualElement _eventContainer;

    private void BuildEventSection(VisualElement parent)
    {
        AddSectionLabel(parent, "ACTIVE WORLD EVENTS");

        _eventContainer = new VisualElement();
        parent.Add(_eventContainer);
    }

    private static void BuildDeferredNote(VisualElement parent)
    {
        var note = new Label(
            "WORLD MAP: 2D political map visualization is deferred pending art asset creation. " +
            "This panel will display region status, active conflict zones, and event geography " +
            "once map art is available.");
        note.AddToClassList("world-deferred-note");
        parent.Add(note);
    }

    public void Refresh()
    {
        if (_eventContainer == null || _worldEventManager == null) return;
        _eventContainer.Clear();

        IReadOnlyList<ActiveWorldEvent> events = _worldEventManager.ActiveEvents;

        if (events.Count == 0)
        {
            var stable = new Label("World stable — no active events.");
            stable.AddToClassList("world-stable-label");
            _eventContainer.Add(stable);
            return;
        }

        foreach (ActiveWorldEvent active in events)
            _eventContainer.Add(BuildEventCard(active));
    }

    private VisualElement BuildEventCard(ActiveWorldEvent active)
    {
        var card = new VisualElement();
        card.AddToClassList("world-event-card");

        var name = new Label(active.EventSO.EventName.ToUpper());
        name.AddToClassList("world-event-name");
        card.Add(name);

        var timer = new Label($"{active.WeeksRemaining} week{(active.WeeksRemaining == 1 ? "" : "s")} remaining");
        timer.AddToClassList("world-event-timer");
        card.Add(timer);

        var desc = new Label(active.EventSO.Description);
        desc.AddToClassList("world-event-desc");
        card.Add(desc);

        // Market effects
        if (active.EventSO.MarketModifiers != null && active.EventSO.MarketModifiers.Length > 0)
        {
            foreach (ContractCategoryModifier mod in active.EventSO.MarketModifiers)
            {
                string demandText = mod.DemandMultiplier >= 1f
                    ? $"+{(mod.DemandMultiplier - 1f) * 100f:F0}% demand"
                    : $"{(mod.DemandMultiplier - 1f) * 100f:F0}% demand";
                string rewardText = mod.RewardMultiplier >= 1f
                    ? $"+{(mod.RewardMultiplier - 1f) * 100f:F0}% reward"
                    : $"{(mod.RewardMultiplier - 1f) * 100f:F0}% reward";

                var effect = new Label($"{mod.ContractCategory}: {demandText}, {rewardText}");
                effect.AddToClassList("world-event-effect");
                card.Add(effect);
            }
        }

        return card;
    }

    private static void AddSectionLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("emp-section-label"); // Reuse existing section label style
        parent.Add(label);
    }
}
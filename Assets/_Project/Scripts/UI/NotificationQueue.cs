using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages transient notification banners in the top-right of the screen.
/// Banners auto-dismiss after 4 seconds using UITK's built-in scheduler.
/// Plain C# class — not a MonoBehaviour.
/// </summary>
public class NotificationQueue
{
    public enum Type { Success, Warning, Failure }

    private readonly VisualElement _container;

    public NotificationQueue(VisualElement documentRoot)
    {
        _container = new VisualElement();
        _container.AddToClassList("notification-container");
        // Added to document root — floats above all layout panels.
        documentRoot.Add(_container);
    }

    public void Show(string title, string body, Type type)
    {
        var banner = new VisualElement();
        banner.AddToClassList("notification-banner");
        banner.AddToClassList(type switch
        {
            Type.Success => "notification-banner--success",
            Type.Warning => "notification-banner--warning",
            Type.Failure => "notification-banner--failure",
            _ => "notification-banner--warning"
        });

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("notification-title");
        banner.Add(titleLabel);

        if (!string.IsNullOrEmpty(body))
        {
            var bodyLabel = new Label(body);
            bodyLabel.AddToClassList("notification-body");
            banner.Add(bodyLabel);
        }

        _container.Add(banner);

        // Trigger slide-in: apply --visible class one frame after insertion.
        // Without the delay, the element starts in its final state with no transition.
        banner.schedule.Execute(() =>
        {
            banner.AddToClassList("notification-banner--visible");
        }).StartingIn(0);   // Zero delay = next frame after layout pass.

        // Auto-dismiss after 4 seconds.
        banner.schedule.Execute(() =>
        {
            if (_container.Contains(banner))
                _container.Remove(banner);
        }).StartingIn(4000);

        Debug.Log($"[Notification] {type}: {title}");
    }
}
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

        // UITK built-in scheduler — no coroutines, no DOTween required.
        banner.schedule.Execute(() =>
        {
            if (_container.Contains(banner))
                _container.Remove(banner);
        }).StartingIn(4000);

        Debug.Log($"[Notification] {type}: {title} — {body}");
    }
}
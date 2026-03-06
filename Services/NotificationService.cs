using Plugin.LocalNotification;

namespace THWTicketApp.Services;

public class NotificationService
{
    private readonly RealtimeService _realtimeService;
    private int _notificationId;

    public bool IsEnabled
    {
        get => Preferences.Get("NotificationsEnabled", true);
        set => Preferences.Set("NotificationsEnabled", value);
    }

    public NotificationService(RealtimeService realtimeService)
    {
        _realtimeService = realtimeService;

        _realtimeService.TicketCreated += OnTicketCreated;
        _realtimeService.TicketUpdated += OnTicketUpdated;
        _realtimeService.CommentAdded += OnCommentAdded;
    }

    private void OnTicketCreated(string ticketId)
    {
        ShowNotification("Neues Ticket", "Ein neues Ticket wurde erstellt.", ticketId);
    }

    private void OnTicketUpdated(string ticketId)
    {
        ShowNotification("Ticket aktualisiert", "Ein Ticket wurde geändert.", ticketId);
    }

    private void OnCommentAdded(string ticketId)
    {
        ShowNotification("Neuer Kommentar", "Ein Kommentar wurde hinzugefügt.", ticketId);
    }

    private void ShowNotification(string title, string description, string ticketId)
    {
        if (!IsEnabled) return;

        // Don't show notifications when app is in foreground and active
        if (IsAppInForeground()) return;

        _notificationId++;

        var request = new NotificationRequest
        {
            NotificationId = _notificationId,
            Title = $"THW Tickets: {title}",
            Description = description,
            CategoryType = NotificationCategoryType.Status,
            ReturningData = ticketId
        };

        LocalNotificationCenter.Current.Show(request);
    }

    private static bool IsAppInForeground()
    {
        try
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window == null) return false;

            // If we can access the window, the app is likely in the foreground
            return window.Page != null;
        }
        catch
        {
            return false;
        }
    }
}

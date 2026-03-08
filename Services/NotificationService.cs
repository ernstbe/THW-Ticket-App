using Plugin.LocalNotification;

namespace THWTicketApp.Services;

public class NotificationService
{
    private readonly RealtimeService _realtimeService;
    private readonly TrueDeskApiService _apiService;
    private int _notificationId;

    public bool IsEnabled
    {
        get => Preferences.Get("NotificationsEnabled", true);
        set => Preferences.Set("NotificationsEnabled", value);
    }

    public bool NotifyOnlyMyTickets
    {
        get => Preferences.Get("NotifyOnlyMyTickets", false);
        set => Preferences.Set("NotifyOnlyMyTickets", value);
    }

    public bool NotifyOnlyHighPriority
    {
        get => Preferences.Get("NotifyOnlyHighPriority", false);
        set => Preferences.Set("NotifyOnlyHighPriority", value);
    }

    public bool NotifyOnNewTickets
    {
        get => Preferences.Get("NotifyOnNewTickets", true);
        set => Preferences.Set("NotifyOnNewTickets", value);
    }

    public bool NotifyOnComments
    {
        get => Preferences.Get("NotifyOnComments", true);
        set => Preferences.Set("NotifyOnComments", value);
    }

    public bool NotifyOnStatusChanges
    {
        get => Preferences.Get("NotifyOnStatusChanges", true);
        set => Preferences.Set("NotifyOnStatusChanges", value);
    }

    public NotificationService(RealtimeService realtimeService, TrueDeskApiService apiService)
    {
        _realtimeService = realtimeService;
        _apiService = apiService;

        _realtimeService.TicketCreated += OnTicketCreated;
        _realtimeService.TicketUpdated += OnTicketUpdated;
        _realtimeService.CommentAdded += OnCommentAdded;
    }

    private void OnTicketCreated(string ticketId)
    {
        if (!NotifyOnNewTickets) return;
        ShowNotification("Neues Ticket", "Ein neues Ticket wurde erstellt.", ticketId);
    }

    private void OnTicketUpdated(string ticketId)
    {
        if (!NotifyOnStatusChanges) return;
        ShowNotification("Ticket aktualisiert", "Ein Ticket wurde geändert.", ticketId);
    }

    private void OnCommentAdded(string ticketId)
    {
        if (!NotifyOnComments) return;
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

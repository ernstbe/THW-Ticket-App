using Plugin.LocalNotification;

namespace THWTicketApp.Services;

public class NotificationService
{
    private readonly RealtimeService _realtimeService;
    private readonly TrueDeskApiService _apiService;
    private readonly DatabaseService _databaseService;
    private int _notificationId;

    public event Action? NotificationReceived;

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

    public NotificationService(RealtimeService realtimeService, TrueDeskApiService apiService, DatabaseService databaseService)
    {
        _realtimeService = realtimeService;
        _apiService = apiService;
        _databaseService = databaseService;

        _realtimeService.TicketCreated += OnTicketCreated;
        _realtimeService.TicketUpdated += OnTicketUpdated;
        _realtimeService.CommentAdded += OnCommentAdded;
    }

    private void OnTicketCreated(string ticketId)
    {
        PersistNotification("Neues Ticket", "Ein neues Ticket wurde erstellt.", "new_ticket", ticketId);
        if (!NotifyOnNewTickets) return;
        ShowNotification("Neues Ticket", "Ein neues Ticket wurde erstellt.", ticketId);
    }

    private void OnTicketUpdated(string ticketId)
    {
        PersistNotification("Ticket aktualisiert", "Ein Ticket wurde geändert.", "status_changed", ticketId);
        if (!NotifyOnStatusChanges) return;
        ShowNotification("Ticket aktualisiert", "Ein Ticket wurde geändert.", ticketId);
    }

    private void OnCommentAdded(string ticketId)
    {
        PersistNotification("Neuer Kommentar", "Ein Kommentar wurde hinzugefügt.", "comment_added", ticketId);
        if (!NotifyOnComments) return;
        ShowNotification("Neuer Kommentar", "Ein Kommentar wurde hinzugefügt.", ticketId);
    }

    private async void PersistNotification(string title, string description, string eventType, string ticketId)
    {
        try
        {
            await _databaseService.AddNotificationAsync(title, description, eventType, ticketId);
            NotificationReceived?.Invoke();
        }
        catch { }
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

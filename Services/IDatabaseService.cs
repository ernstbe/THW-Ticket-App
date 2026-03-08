using THWTicketApp.Data;
using THWTicketApp.Models;

namespace THWTicketApp.Services;

public interface IDatabaseService
{
    Task<List<CachedTicket>> GetCachedTicketsAsync();
    Task<CachedTicket?> GetCachedTicketAsync(string id);
    Task SaveTicketsAsync(IEnumerable<Ticket> tickets);
    Task<List<Ticket>> GetTicketsFromCacheAsync();
    Task ClearCacheAsync();
    Task<DateTime?> GetLastCacheTimeAsync();
    Task<int> GetCachedTicketCountAsync();

    // Pending Actions Queue
    Task EnqueueActionAsync(string actionType, string payloadJson, DateTime? ticketUpdatedAt = null);
    Task MarkActionConflictedAsync(int id, string reason);
    Task<List<PendingAction>> GetPendingActionsAsync();
    Task<int> GetPendingActionCountAsync();
    Task RemoveActionAsync(int id);
    Task IncrementRetryCountAsync(int id);

    // Favorites
    Task<bool> IsFavoriteAsync(string ticketId);
    Task ToggleFavoriteAsync(string ticketId);
    Task<HashSet<string>> GetFavoriteIdsAsync();

    // Time Tracking
    Task<TimeEntry> StartTimerAsync(string ticketId);
    Task StopTimerAsync(int entryId, string? description = null);
    Task<TimeEntry?> GetActiveTimerAsync(string ticketId);
    Task<List<TimeEntry>> GetTimeEntriesAsync(string ticketId);
    Task<double> GetTotalTimeAsync(string ticketId);
    Task DeleteTimeEntryAsync(int entryId);

    // Linked Tickets
    Task AddLinkedTicketAsync(string sourceId, string linkedId, string linkedSubject, int linkedUid, string linkType = "related");
    Task<List<LinkedTicket>> GetLinkedTicketsAsync(string ticketId);
    Task RemoveLinkedTicketAsync(int linkId);

    // Notification History
    Task AddNotificationAsync(string title, string description, string eventType, string ticketId);
    Task<List<NotificationEntry>> GetNotificationsAsync(int limit = 50);
    Task<int> GetUnreadNotificationCountAsync();
    Task MarkNotificationReadAsync(int id);
    Task MarkAllNotificationsReadAsync();
    Task ClearNotificationHistoryAsync();

    // Saved Filters
    Task<int> SaveFilterAsync(SavedFilter filter);
    Task<List<SavedFilter>> GetSavedFiltersAsync();
    Task DeleteSavedFilterAsync(int id);
}

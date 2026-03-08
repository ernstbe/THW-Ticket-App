using THWTicketApp.Data;

namespace THWTicketApp.Services;

public interface ISyncService
{
    event Action<int>? PendingCountChanged;
    event Action<PendingAction>? ConflictDetected;

    Task<int> GetPendingCountAsync();
    Task EnqueueCommentAsync(string ticketId, string ownerId, string comment, DateTime? ticketUpdatedAt = null);
    Task EnqueueNoteAsync(string ticketId, string ownerId, string note, DateTime? ticketUpdatedAt = null);
    Task EnqueueCreateTicketAsync(string subject, string? issue, string? typeId, string? priorityId, string? groupId, string? assigneeId);
    Task EnqueueAssignAsync(string ticketId, string userId, DateTime? ticketUpdatedAt = null);
    Task<bool> SyncPendingActionsAsync();
    Task<bool> ForceApplyAsync(int actionId);
    Task DiscardActionAsync(int actionId);
    Task<List<PendingAction>> GetConflictedActionsAsync();
}

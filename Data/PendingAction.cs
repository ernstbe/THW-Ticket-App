using SQLite;

namespace THWTicketApp.Data;

[Table("PendingActions")]
public class PendingAction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ActionType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }

    /// <summary>Ticket's Updated timestamp at the time the action was queued (null for CreateTicket).</summary>
    public DateTime? TicketUpdatedAt { get; set; }

    /// <summary>True if a conflict was detected during sync (server ticket was modified since queuing).</summary>
    public bool IsConflicted { get; set; }

    /// <summary>Human-readable conflict description.</summary>
    public string? ConflictReason { get; set; }
}

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
}

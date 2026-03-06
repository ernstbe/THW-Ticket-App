using SQLite;

namespace THWTicketApp.Data;

[Table("LinkedTickets")]
public class LinkedTicket
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string SourceTicketId { get; set; } = string.Empty;
    public string LinkedTicketId { get; set; } = string.Empty;
    public string LinkedTicketSubject { get; set; } = string.Empty;
    public int LinkedTicketUid { get; set; }
    public string LinkType { get; set; } = "related"; // related, blocks, blocked_by, duplicate
    public DateTime CreatedAt { get; set; }
}

using SQLite;

namespace THWTicketApp.Data;

[Table("FavoriteTickets")]
public class FavoriteTicket
{
    [PrimaryKey]
    public string TicketId { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

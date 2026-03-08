using SQLite;

namespace THWTicketApp.Data;

[Table("SavedFilters")]
public class SavedFilter
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

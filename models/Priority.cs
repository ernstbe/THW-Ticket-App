namespace THWTicketApp.Models;

public class Priority
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int OverdueIn { get; set; }
    public string HtmlColor { get; set; }
    public int MigrationNum { get; set; }
    public bool Default { get; set; }
    public int __v { get; set; }
    public string DurationFormatted { get; set; }
}
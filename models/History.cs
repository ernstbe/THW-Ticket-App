namespace THWTicketApp.Models;

public class History
{
    public string Action { get; set; }
    public string Date { get; set; }
    public User Owner { get; set; }
    public string Description { get; set; }
    public string Id { get; set; }
}
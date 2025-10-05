namespace THWTicketApp.Models;
public class Status
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string HtmlColor { get; set; }
    public int Uid { get; set; }
    public int Order { get; set; }
    public bool Slatimer { get; set; }
    public bool IsResolved { get; set; }
    public bool IsLocked { get; set; }
    public int __v { get; set; }
}
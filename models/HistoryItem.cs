using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class HistoryItem : BindableObject
{
<<<<<<< HEAD
    public string Action { get; set; }
    public DateTime Date { get; set; }
    public Owner Owner { get; set; }
    public string Description { get; set; }
    [JsonPropertyName("_id")]
    public string Id { get; set; }
=======
    public string? Action { get; set; }
    public DateTime Date { get; set; }
    public Owner? Owner { get; set; }
    public string? Description { get; set; }
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
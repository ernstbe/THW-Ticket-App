using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class HistoryItem : BindableObject
{
    public string Action { get; set; }
    public DateTime Date { get; set; }
    public Owner Owner { get; set; }
    public string Description { get; set; }
    [JsonPropertyName("_id")]
    public string Id { get; set; }
}
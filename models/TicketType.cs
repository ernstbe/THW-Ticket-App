using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class TicketType : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Priority> Priorities { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
}

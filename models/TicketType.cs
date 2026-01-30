using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class TicketType : BindableObject
{
    [JsonPropertyName("_id")]
<<<<<<< HEAD
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Priority> Priorities { get; set; }
=======
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<Priority> Priorities { get; set; } = [];
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    [JsonPropertyName("__v")]
    public int Version { get; set; }
}

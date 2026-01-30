using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Tag : BindableObject
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Normalized { get; set; }

    [JsonPropertyName("__v")]
    public int Version { get; set; }
}
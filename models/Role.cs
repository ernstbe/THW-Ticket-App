using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Role : BindableObject
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Normalized { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsAgent { get; set; }

    [JsonPropertyName("_id")]
    public string? InternalId { get; set; }
}
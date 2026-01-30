using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Status : BindableObject
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? HtmlColor { get; set; }
    public int Uid { get; set; }
    public int Order { get; set; }
    public bool Slatimer { get; set; }
    public bool IsResolved { get; set; }
    public bool IsLocked { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
    public string? Id2 { get; set; }
}
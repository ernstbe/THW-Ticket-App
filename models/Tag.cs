using System.Text.Json.Serialization;

public class Tag : BindableObject
{
    [JsonPropertyName("_id")]
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? Normalized { get; set; }

    [JsonPropertyName("__v")]
    public int Version { get; set; }

}
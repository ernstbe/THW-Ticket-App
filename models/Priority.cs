using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Priority : BindableObject
{
    [JsonPropertyName("_id")]
<<<<<<< HEAD
    public string Id { get; set; }
    public string Name { get; set; }
    public int OverdueIn { get; set; }
    public string HtmlColor { get; set; }
=======
    public string? Id { get; set; }
    public string? Name { get; set; }
    public int OverdueIn { get; set; }
    public string? HtmlColor { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    public int MigrationNum { get; set; }
    public bool Default { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
<<<<<<< HEAD
    public string DurationFormatted { get; set; }
    public string Id2 { get; set; }
=======
    public string? DurationFormatted { get; set; }
    public string? Id2 { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
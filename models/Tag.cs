using System.Text.Json.Serialization;

<<<<<<< HEAD
public class Tag : BindableObject
{
    [JsonPropertyName("_id")]
    public required string Id { get; set; }
=======
namespace THWTicketApp.Models;

public class Tag : BindableObject
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    public string? Name { get; set; }
    public string? Normalized { get; set; }

    [JsonPropertyName("__v")]
    public int Version { get; set; }
<<<<<<< HEAD

=======
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
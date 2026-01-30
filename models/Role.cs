using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Role : BindableObject
{
<<<<<<< HEAD
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Normalized { get; set; }
=======
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Normalized { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    public bool IsAdmin { get; set; }
    public bool IsAgent { get; set; }

    [JsonPropertyName("_id")]
<<<<<<< HEAD
    public string InternalId { get; set; }
=======
    public string? InternalId { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
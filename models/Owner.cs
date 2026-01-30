using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Owner : BindableObject
{
    [JsonPropertyName("_id")]
<<<<<<< HEAD
    public string Id { get; set; }
    public string Username { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Title { get; set; }
=======
    public string Id { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Fullname { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Title { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
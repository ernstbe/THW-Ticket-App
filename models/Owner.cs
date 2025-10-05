using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Owner : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    public string Username { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Title { get; set; }
}
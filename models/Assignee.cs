using System.Text.Json.Serialization;
using THWTicketApp.Models;

<<<<<<< HEAD
public class Assignee : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    public string Username { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }
    public Role Role { get; set; }
    public string Title { get; set; }
=======
namespace THWTicketApp.Models;

public class Assignee : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Fullname { get; set; }
    public string? Email { get; set; }
    public Role? Role { get; set; }
    public string? Title { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    public bool Deleted { get; set; }
}
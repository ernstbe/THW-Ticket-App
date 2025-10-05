using System.Text.Json.Serialization;
using THWTicketApp.Models;

public class Assignee : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    public string Username { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }
    public Role Role { get; set; }
    public string Title { get; set; }
}
using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Group : BindableObject
{
    [JsonPropertyName("_id")]
<<<<<<< HEAD
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Assignee> Members { get; set; }
    public List<string> SendMailTo { get; set; }
=======
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<Assignee> Members { get; set; } = [];
    public List<string> SendMailTo { get; set; } = [];
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    public bool Public { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
}
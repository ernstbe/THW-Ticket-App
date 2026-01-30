using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Group : BindableObject
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<Assignee> Members { get; set; } = [];
    public List<string> SendMailTo { get; set; } = [];
    public bool Public { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
}
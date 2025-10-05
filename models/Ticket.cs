namespace THWTicketApp.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Ticket : BindableObject
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    public Group Group { get; set; }
    public bool Deleted { get; set; }
    public TicketType Type { get; set; }
    public Priority Priority { get; set; }
    public List<string> Tags { get; set; }
    public string Subject { get; set; }
    public string Issue { get; set; }
    public List<string> Subscribers { get; set; }
    public DateTime Date { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Note> Notes { get; set; }
    public List<Attachment> Attachments { get; set; }
    public List<HistoryItem> History { get; set; }
    public Status Status { get; set; }
    public Owner Owner { get; set; }
    public int Uid { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
}
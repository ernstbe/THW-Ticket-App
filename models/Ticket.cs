namespace THWTicketApp.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public class Ticket
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    public Group? Group { get; set; }
    public bool Deleted { get; set; }
    public TicketType? Type { get; set; }
    public Priority? Priority { get; set; }
    public List<Tag> Tags { get; set; } = new();
    public string? Subject { get; set; }
    public string? Issue { get; set; }
    public List<string> Subscribers { get; set; } = new();
    public DateTime Date { get; set; }
    public ObservableCollection<Comment> Comments { get; set; } = new();
    public List<Note> Notes { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public List<HistoryItem> History { get; set; } = new();
    public Status? Status { get; set; }
    public Owner? Owner { get; set; }
    public int Uid { get; set; }
    [JsonPropertyName("__v")]
    public int Version { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public Assignee? Assignee { get; set; }
    public DateTime Updated { get; set; }
}
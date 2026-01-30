using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Comment : BindableObject
{
<<<<<<< HEAD
  [JsonPropertyName("_id")]
  public string Id { get; set; }
  public DateTime Date { get; set; }
  public Assignee Owner { get; set; }
  [JsonPropertyName("comment")]
  public string Text { get; set; }
  public bool Deleted { get; set; }
=======
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public DateTime Date { get; set; }
    public Assignee? Owner { get; set; }
    [JsonPropertyName("comment")]
    public string? Text { get; set; }
    public bool Deleted { get; set; }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}

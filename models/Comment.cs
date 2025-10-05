using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Comment : BindableObject
{
  [JsonPropertyName("_id")]
  public string Id { get; set; }
  public DateTime Date { get; set; }
  public Owner Owner { get; set; }
  [JsonPropertyName("comment")]
  public string Text { get; set; }
  public bool Deleted { get; set; }
}

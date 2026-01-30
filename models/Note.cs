<<<<<<< HEAD
namespace THWTicketApp.Models;
public class Note { }
=======
using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Note
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("note")]
    public string? Content { get; set; }
}
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

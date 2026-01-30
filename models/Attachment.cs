<<<<<<< HEAD
namespace THWTicketApp.Models;

    public class Attachment{ }
=======
using System.Text.Json.Serialization;

namespace THWTicketApp.Models;

public class Attachment
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("uploadDate")]
    public DateTime? UploadDate { get; set; }
}
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

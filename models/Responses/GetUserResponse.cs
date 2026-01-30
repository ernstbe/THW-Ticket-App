namespace THWTicketApp.Models.Responses;

public class GetUserResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
<<<<<<< HEAD
    public List<User> Users { get; set; }

=======
    public List<User> Users { get; set; } = [];
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}
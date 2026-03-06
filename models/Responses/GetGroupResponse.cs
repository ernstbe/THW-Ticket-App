namespace THWTicketApp.Models.Responses;

public class GetGroupResponse
{
    public bool Success { get; set; }
    public List<Group> Groups { get; set; } = [];
}

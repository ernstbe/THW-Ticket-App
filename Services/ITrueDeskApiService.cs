using THWTicketApp.Models;

namespace THWTicketApp.Services;

public interface ITrueDeskApiService
{
    string? CurrentUsername { get; }
    string? CurrentUserId { get; }
    string? LastError { get; }
    bool IsAuthenticated { get; }

    Task<bool> AuthenticateAsync(string username, string password);
    Task<bool> TryRestoreSessionAsync();
    void Logout();

    Task<string> GetTicketsAsync();
    Task<string> GetTicketsPagedAsync(int page = 0, int limit = 50);
    Task<string> SearchTicketsAsync(string query);
    Task<string> AddTicketAsync(string title, string description, int assignedUserId);
    Task<bool> AssignTicketAsync(string ticketId, string userId);
    Task<bool> ClearTicketAssigneeAsync(string ticketId);
    Task<bool> AddCommentAsync(string id, string ownerId, string newComment);
    Task<string> GetTicketAsync(string ticketId);
    Task<bool> EditTicketAsync(Ticket ticket);
    Task<bool> AddNoteAsync(string ticketId, string ownerId, string note);
    Task<string> GetStatusesAsync();
    Task<string> GetUsersAsync();
    Task<string> GetTicketTypesAsync();
    Task<string> GetTagsAsync();
    Task<string> GetGroupsAsync();
    Task<bool> UploadAttachmentAsync(string ticketId, Stream fileStream, string fileName);
    Task<Stream?> DownloadAttachmentAsync(string attachmentPath);
    string GetAttachmentUrl(string attachmentPath);
    Task<bool> DeleteAttachmentAsync(string ticketId, string attachmentId);
    Task<bool> UpdateTicketStatusAsync(string ticketId, string statusId);
    Task<bool> CreateTicketAsync(string subject, string? issue, string? typeId, string? priorityId, string? groupId, string? assigneeId);
}

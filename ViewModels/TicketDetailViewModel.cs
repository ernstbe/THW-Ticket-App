using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using THWTicketApp.Models.Responses;

namespace THWTicketApp.ViewModels
{
    public partial class TicketDetailViewModel : ObservableObject
    {
        [ObservableProperty] private Ticket ticket;
        [ObservableProperty] private string newComment;
        [ObservableProperty] private string editSubject;
        [ObservableProperty] private string editIssue;
        [ObservableProperty] private string statusMessage;
    [ObservableProperty] private User? selectedAssignee;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        private readonly TrueDeskApiService _apiService;

        public TicketDetailViewModel(Ticket ticket, TrueDeskApiService apiService)
        {
            Ticket = ticket;
            _apiService = apiService;
            NewComment = string.Empty;
            StatusMessage = string.Empty;
            SelectedAssignee = null;
            EditSubject = ticket?.Subject ?? string.Empty;
            EditIssue = ticket?.Issue ?? string.Empty;
        }

        public async Task LoadUsers()
        {

            var json = await _apiService.GetUsers();
            Users.Clear();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var userResponse = System.Text.Json.JsonSerializer.Deserialize<GetUserResponse>(json, options);

            if (userResponse != null)
            {
                if (userResponse.Count == 0)
                {
                    StatusMessage = "No users found.";
                    return;
                }

                foreach (var user in userResponse.Users)
                {
                    Users.Add(user);
                }
            }

        }

        [RelayCommand]
        private async Task Assign()
        {
            if (SelectedAssignee == null)
            {
                StatusMessage = "No assignee selected.";
                return;
            }

            var success = await _apiService.AssignTicketAsync(Ticket.Id, SelectedAssignee.Id);
            StatusMessage = success ? "Assignment updated." : "Assignment failed.";
            if (success)
            {
                // reload ticket
                var json = await _apiService.GetTicketAsync(Ticket.Id);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                if (updated != null)
                {
                    Ticket = updated;
                    THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                }
            }
        }

        [RelayCommand]
        private async Task ClearAssignee()
        {
            var success = await _apiService.ClearTicketAssigneeAsync(Ticket.Id);
            if (success)
            {
                Ticket.Assignee = null;
                StatusMessage = "Assignee cleared.";
                var json = await _apiService.GetTicketAsync(Ticket.Id);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                if (updated != null)
                {
                    Ticket = updated;
                    THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                }
            }
            else
            {
                StatusMessage = "Failed to clear assignee.";
            }
        }

        [RelayCommand]
        private async Task AddComment()
        {
            if (!string.IsNullOrWhiteSpace(NewComment))
            {
                var ownerId = Ticket.Owner?.Id;
                var comment = false;
                if (!string.IsNullOrWhiteSpace(ownerId))
                {
                    comment = await _apiService.AddCommentAsync(Ticket.Id, ownerId, NewComment);
                }
                if (comment)
                {
                    StatusMessage = "Comment added.";
                    var json = await _apiService.GetTicketAsync(Ticket.Id);
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                        if (updated != null)
                        {
                            Ticket = updated;
                            THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                        }
                }
                else
                {
                    StatusMessage = "Failed to add comment.";
                }
                // if (comment != null)
                // {
                //     Ticket.Comments.Add(comment);
                //     NewComment = string.Empty;
                //     StatusMessage = "Comment added.";
                // }
                // else
                // {
                //     StatusMessage = "Failed to add comment.";
                // }
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            Ticket.Subject = EditSubject;
            Ticket.Issue = EditIssue;
            var success = await _apiService.EditTicketAsync(Ticket);
            StatusMessage = success ? "Ticket updated." : "Update failed.";
        }
    }
}

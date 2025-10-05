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
        [ObservableProperty] private User selectedAssignee;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        private readonly TrueDeskApiService _apiService;

        public TicketDetailViewModel(Ticket ticket, TrueDeskApiService apiService)
        {
            Ticket = ticket;
            _apiService = apiService;
            EditSubject = ticket.Subject;
            EditIssue = ticket.Issue;
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
            var success = await _apiService.AssignTicketAsync(Ticket.Id, SelectedAssignee.Id);
            StatusMessage = success ? "Assignment updated." : "Assignment failed.";
        }

        [RelayCommand]
        private async Task AddComment()
        {
            if (!string.IsNullOrWhiteSpace(NewComment))
            {
                var comment = await _apiService.AddCommentAsync(Ticket.Id, Ticket.Owner.Id, NewComment);
                if (comment == true)
                {
                    StatusMessage = "Comment added.";
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

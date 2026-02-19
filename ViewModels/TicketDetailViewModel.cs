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
        [ObservableProperty]
        private Ticket? _ticket;

        [ObservableProperty]
        private string _newComment = string.Empty;

        [ObservableProperty]
        private string _editSubject = string.Empty;

        [ObservableProperty]
        private string _editIssue = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private User? _selectedAssignee;

        [ObservableProperty]
        private ObservableCollection<User> _users = [];

        [ObservableProperty]
        private bool _isLoading;

        private readonly TrueDeskApiService _apiService;

        public TicketDetailViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
        }

        public void SetTicket(Ticket ticket)
        {
            Ticket = ticket;
            EditSubject = ticket?.Subject ?? string.Empty;
            EditIssue = ticket?.Issue ?? string.Empty;
        }

        public async Task LoadUsersAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = string.Empty;

            try
            {
                var json = await _apiService.GetUsersAsync();
                Users.Clear();

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var userResponse = System.Text.Json.JsonSerializer.Deserialize<GetUserResponse>(json, options);

                if (userResponse?.Users != null)
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
            catch (Exception)
            {
                StatusMessage = "Failed to load users.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AssignAsync()
        {
            if (Ticket == null || SelectedAssignee == null)
            {
                StatusMessage = "Please select an assignee.";
                return;
            }

            IsLoading = true;
            try
            {
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
            catch (Exception)
            {
                StatusMessage = "Failed to update assignment.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClearAssigneeAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "No ticket selected.";
                return;
            }

            IsLoading = true;
            try
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
            catch (Exception)
            {
                StatusMessage = "Error clearing assignee.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddCommentAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "No ticket selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewComment))
            {
                StatusMessage = "Please enter a comment.";
                return;
            }

            IsLoading = true;
            try
            {
                var ownerId = Ticket.Owner?.Id ?? string.Empty;
                var success = await _apiService.AddCommentAsync(Ticket.Id, ownerId, NewComment);

                if (success)
                {
                    StatusMessage = "Comment added.";
                    NewComment = string.Empty;
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
            }
            catch (Exception)
            {
                StatusMessage = "Error adding comment.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task EditAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "No ticket selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditSubject))
            {
                StatusMessage = "Subject is required.";
                return;
            }

            IsLoading = true;
            try
            {
                Ticket.Subject = EditSubject;
                Ticket.Issue = EditIssue;
                var success = await _apiService.EditTicketAsync(Ticket);
                StatusMessage = success ? "Ticket updated." : "Update failed.";
            }
            catch (Exception)
            {
                StatusMessage = "Error updating ticket.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

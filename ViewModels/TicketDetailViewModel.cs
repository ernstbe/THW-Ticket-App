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
        private Ticket? _ticket;
        public Ticket? Ticket
        {
            get => _ticket;
            set => SetProperty(ref _ticket, value);
        }

        private string _newComment = string.Empty;
        public string NewComment
        {
            get => _newComment;
            set => SetProperty(ref _newComment, value);
        }

        private string _editSubject = string.Empty;
        public string EditSubject
        {
            get => _editSubject;
            set => SetProperty(ref _editSubject, value);
        }

        private string _editIssue = string.Empty;
        public string EditIssue
        {
            get => _editIssue;
            set => SetProperty(ref _editIssue, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private User? _selectedAssignee;
        public User? SelectedAssignee
        {
            get => _selectedAssignee;
            set => SetProperty(ref _selectedAssignee, value);
        }

        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private readonly TrueDeskApiService _apiService;

        public TicketDetailViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
        }

        public void SetTicket(Ticket ticket)
        {
            Ticket = ticket;
            EditSubject = ticket.Subject ?? string.Empty;
            EditIssue = ticket.Issue ?? string.Empty;
        }

        public async Task LoadUsers()
        {
            try
            {
                var json = await _apiService.GetUsersAsync();
                Users.Clear();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private User? _selectedAssignee;
        public User? SelectedAssignee
        {
            get => _selectedAssignee;
            set => SetProperty(ref _selectedAssignee, value);
        }

        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        public TicketDetailViewModel(Ticket ticket, TrueDeskApiService apiService)
        private readonly TrueDeskApiService _apiService;
        {
        public TicketDetailViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
        }
        public async Task LoadUsers()
        public void SetTicket(Ticket ticket)
        {
            _ticket = ticket;
            _editSubject = ticket.Subject ?? string.Empty;
            _editIssue = ticket.Issue ?? string.Empty;
        }
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

                var json = await _apiService.GetUsersAsync();
                _users.Clear();
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
=======
        [ObservableProperty]
        private Ticket? _ticket;
=======
    [ObservableProperty]
    private Ticket? _ticket;
>>>>>>> b53bf30 (Implement ticket assignment and clearing functionality; update TicketDetailPage and TicketDetailViewModel for enhanced user interaction and data handling)

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
        private async Task AssignAsync()
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
        private async Task AddCommentAsync()
            }
<<<<<<< HEAD
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
            }
            catch (Exception)
            {
                StatusMessage = "Failed to update assignment.";
            }
            finally
            {
                IsLoading = false;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
            }
        }

        [RelayCommand]
<<<<<<< HEAD
        private async Task ClearAssignee()
        private async Task EditAsync()
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
=======
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
<<<<<<< HEAD
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
=======
=======

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
>>>>>>> c547f7b (Implement ticket assignment and clearing functionality; update TicketDetailPage and TicketDetailViewModel for enhanced user interaction and data handling)
>>>>>>> b53bf30 (Implement ticket assignment and clearing functionality; update TicketDetailPage and TicketDetailViewModel for enhanced user interaction and data handling)
                }
                else
                {
                    StatusMessage = "Failed to add comment.";
                }
<<<<<<< HEAD
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
=======
            }
            catch (Exception)
            {
                StatusMessage = "Error adding comment.";
            }
            finally
            {
                IsLoading = false;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
            }
        }

        [RelayCommand]
<<<<<<< HEAD
        private async Task Edit()
        {
            Ticket.Subject = EditSubject;
            Ticket.Issue = EditIssue;
            var success = await _apiService.EditTicketAsync(Ticket);
            StatusMessage = success ? "Ticket updated." : "Update failed.";
=======
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
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }
    }
}

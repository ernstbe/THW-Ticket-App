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
        private ObservableCollection<Status> _statuses = [];

        [ObservableProperty]
        private Status? _selectedStatus;

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
            EditSubject = ticket.Subject ?? string.Empty;
            EditIssue = ticket.Issue ?? string.Empty;
            // Set current status as selected
            if (ticket.Status != null && Statuses.Count > 0)
            {
                SelectedStatus = Statuses.FirstOrDefault(s => s.Id == ticket.Status.Id);
            }
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

        public async Task LoadStatusesAsync()
        {
            try
            {
                var json = await _apiService.GetStatusesAsync();
                Statuses.Clear();

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var statusResponse = System.Text.Json.JsonSerializer.Deserialize<GetStatusResponse>(json, options);

                if (statusResponse?.Statuses != null)
                {
                    foreach (var status in statusResponse.Statuses)
                    {
                        Statuses.Add(status);
                    }
                }

                // Set current ticket status as selected after items are loaded
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Ticket?.Status?.Id != null)
                    {
                        SelectedStatus = Statuses.FirstOrDefault(s => s.Id == Ticket.Status.Id);
                    }
                });
            }
            catch (Exception)
            {
                StatusMessage = "Failed to load statuses.";
            }
        }

        [RelayCommand]
        private async Task ChangeStatusAsync()
        {
            if (Ticket == null || SelectedStatus == null)
            {
                StatusMessage = "Bitte wählen Sie einen Status.";
                return;
            }

            IsLoading = true;
            try
            {
                var success = await _apiService.UpdateTicketStatusAsync(Ticket.Id, SelectedStatus.Id);
                if (success)
                {
                    Ticket.Status = SelectedStatus;
                    OnPropertyChanged(nameof(Ticket));
                    StatusMessage = "Status aktualisiert.";
                }
                else
                {
                    StatusMessage = "Status-Änderung fehlgeschlagen.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Fehler beim Ändern des Status.";
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
                if (success)
                {
                    Ticket.Assignee = new Assignee
                    {
                        Id = SelectedAssignee.Id,
                        Fullname = SelectedAssignee.Fullname
                    };
                    OnPropertyChanged(nameof(Ticket));
                    StatusMessage = "Zuweisung aktualisiert.";
                }
                else
                {
                    StatusMessage = "Zuweisung fehlgeschlagen.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Fehler bei der Zuweisung.";
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
                var success = await _apiService.AddCommentAsync(Ticket.Id, NewComment);

                if (success)
                {
                    StatusMessage = "Kommentar hinzugefügt.";
                    NewComment = string.Empty;
                    OnPropertyChanged(nameof(Ticket));
                }
                else
                {
                    StatusMessage = "Kommentar konnte nicht hinzugefügt werden.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Fehler beim Hinzufügen des Kommentars.";
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;
using System.Collections.ObjectModel;
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
            TrudeskTranslationHelper.TranslateTicket(ticket);
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
                        StatusMessage = "Keine Benutzer gefunden.";
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
                StatusMessage = "Fehler beim Laden der Benutzer.";
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
                StatusMessage = "Bitte einen Zuständigen auswählen.";
                return;
            }

            IsLoading = true;
            try
            {
                var success = await _apiService.AssignTicketAsync(Ticket.Id, SelectedAssignee.Id);
                StatusMessage = success ? "Zuweisung aktualisiert." : "Zuweisung fehlgeschlagen.";
                if (success)
                {
                    // reload ticket
                    var json = await _apiService.GetTicketAsync(Ticket.Id);
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                    if (updated != null)
                    {
                        TrudeskTranslationHelper.TranslateTicket(updated);
                        Ticket = updated;
                        THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                    }
                }
            }
            catch (Exception)
            {
                StatusMessage = "Fehler beim Aktualisieren der Zuweisung.";
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
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            IsLoading = true;
            try
            {
                var success = await _apiService.ClearTicketAssigneeAsync(Ticket.Id);
                if (success)
                {
                    Ticket.Assignee = null;
                    StatusMessage = "Zuweisung entfernt.";
                    var json = await _apiService.GetTicketAsync(Ticket.Id);
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                    if (updated != null)
                    {
                        TrudeskTranslationHelper.TranslateTicket(updated);
                        Ticket = updated;
                        THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                    }
                }
                else
                {
                    StatusMessage = "Fehler beim Entfernen der Zuweisung.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Fehler beim Entfernen der Zuweisung.";
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
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewComment))
            {
                StatusMessage = "Bitte einen Kommentar eingeben.";
                return;
            }

            IsLoading = true;
            try
            {
                var ownerId = Ticket.Owner?.Id ?? string.Empty;
                var success = await _apiService.AddCommentAsync(Ticket.Id, ownerId, NewComment);

                if (success)
                {
                    StatusMessage = "Kommentar hinzugefügt.";
                    NewComment = string.Empty;
                    var json = await _apiService.GetTicketAsync(Ticket.Id);
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                    if (updated != null)
                    {
                        TrudeskTranslationHelper.TranslateTicket(updated);
                        Ticket = updated;
                        THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                    }
                }
                else
                {
                    StatusMessage = "Fehler beim Hinzufügen des Kommentars.";
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
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditSubject))
            {
                StatusMessage = "Betreff ist erforderlich.";
                return;
            }

            IsLoading = true;
            try
            {
                Ticket.Subject = EditSubject;
                Ticket.Issue = EditIssue;
                var success = await _apiService.EditTicketAsync(Ticket);
                StatusMessage = success ? "Ticket aktualisiert." : "Aktualisierung fehlgeschlagen.";
            }
            catch (Exception)
            {
                StatusMessage = "Fehler beim Aktualisieren des Tickets.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

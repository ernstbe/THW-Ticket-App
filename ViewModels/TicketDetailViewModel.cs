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

        [ObservableProperty]
        private ObservableCollection<Status> _statuses = [];

        [ObservableProperty]
        private ObservableCollection<Priority> _priorities = [];

        [ObservableProperty]
        private Status? _selectedStatus;

        [ObservableProperty]
        private Priority? _selectedPriority;

        [ObservableProperty]
        private string _newNote = string.Empty;

        [ObservableProperty]
        private int _pendingActionsCount;

        private readonly TrueDeskApiService _apiService;
        private readonly SyncService _syncService;

        public TicketDetailViewModel(TrueDeskApiService apiService, SyncService syncService)
        {
            _apiService = apiService;
            _syncService = syncService;
            _syncService.PendingCountChanged += count =>
                MainThread.BeginInvokeOnMainThread(() => PendingActionsCount = count);
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
                await Task.WhenAll(
                    LoadUsersInternalAsync(),
                    LoadStatusesAsync(),
                    LoadPrioritiesAsync()
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadUsersInternalAsync()
        {
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
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Benutzer: {message}";
            }
        }

        private async Task LoadStatusesAsync()
        {
            try
            {
                var json = await _apiService.GetStatusesAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var statusList = System.Text.Json.JsonSerializer.Deserialize<Status[]>(json, options);

                Statuses.Clear();
                if (statusList != null)
                {
                    foreach (var status in statusList)
                    {
                        status.Name = TrudeskTranslationHelper.TranslateStatus(status.Name);
                        Statuses.Add(status);
                    }
                }

                // Pre-select current status
                if (Ticket?.Status != null)
                {
                    SelectedStatus = Statuses.FirstOrDefault(s => s.Id == Ticket.Status.Id);
                }
            }
            catch
            {
                // Statuses are optional - edit will still work without them
            }
        }

        private async Task LoadPrioritiesAsync()
        {
            try
            {
                var json = await _apiService.GetTicketTypesAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var types = System.Text.Json.JsonSerializer.Deserialize<TicketType[]>(json, options);

                Priorities.Clear();
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        if (type.Priorities != null)
                        {
                            foreach (var priority in type.Priorities)
                            {
                                if (!Priorities.Any(p => p.Id == priority.Id))
                                {
                                    priority.Name = TrudeskTranslationHelper.TranslatePriority(priority.Name);
                                    Priorities.Add(priority);
                                }
                            }
                        }
                    }
                }

                // Pre-select current priority
                if (Ticket?.Priority != null)
                {
                    SelectedPriority = Priorities.FirstOrDefault(p => p.Id == Ticket.Priority.Id);
                }
            }
            catch
            {
                // Priorities are optional
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
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Status: {message}";
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
                    await ReloadTicketAsync();
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Zuweisung: {message}";
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
                    StatusMessage = "Zuweisung entfernt.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Fehler beim Entfernen der Zuweisung.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Zuweisung entfernen: {message}";
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
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Fehler beim Hinzufügen des Kommentars.";
                }
            }
            catch (Exception ex)
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    var ownerId = Ticket.Owner?.Id ?? string.Empty;
                    await _syncService.EnqueueCommentAsync(Ticket.Id, ownerId, NewComment);
                    StatusMessage = "Offline: Kommentar wird bei Verbindung gesendet.";
                    NewComment = string.Empty;
                }
                else
                {
                    var (message, _) = Utils.ErrorHelper.Categorize(ex);
                    StatusMessage = $"Kommentar: {message}";
                }
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

                if (SelectedStatus != null)
                    Ticket.Status = SelectedStatus;
                if (SelectedPriority != null)
                    Ticket.Priority = SelectedPriority;

                var success = await _apiService.EditTicketAsync(Ticket);
                if (success)
                {
                    StatusMessage = "Ticket aktualisiert.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Aktualisierung fehlgeschlagen.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Bearbeitung: {message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddNoteAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewNote))
            {
                StatusMessage = "Bitte eine Notiz eingeben.";
                return;
            }

            IsLoading = true;
            try
            {
                var ownerId = Ticket.Owner?.Id ?? string.Empty;
                var success = await _apiService.AddNoteAsync(Ticket.Id, ownerId, NewNote);

                if (success)
                {
                    StatusMessage = "Notiz hinzugefügt.";
                    NewNote = string.Empty;
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Fehler beim Hinzufügen der Notiz.";
                }
            }
            catch (Exception ex)
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    var ownerId = Ticket.Owner?.Id ?? string.Empty;
                    await _syncService.EnqueueNoteAsync(Ticket.Id, ownerId, NewNote);
                    StatusMessage = "Offline: Notiz wird bei Verbindung gesendet.";
                    NewNote = string.Empty;
                }
                else
                {
                    var (message, _) = Utils.ErrorHelper.Categorize(ex);
                    StatusMessage = $"Notiz: {message}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UploadAttachmentAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Datei auswählen"
                });

                if (result == null) return;

                IsLoading = true;
                StatusMessage = "Datei wird hochgeladen...";

                using var stream = await result.OpenReadAsync();
                var success = await _apiService.UploadAttachmentAsync(Ticket.Id, stream, result.FileName);

                if (success)
                {
                    StatusMessage = "Datei hochgeladen.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Upload fehlgeschlagen.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Upload: {message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DownloadAttachmentAsync(Models.Attachment attachment)
        {
            if (attachment?.Path == null)
            {
                StatusMessage = "Kein Dateipfad verfügbar.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Datei wird heruntergeladen...";

            try
            {
                using var stream = await _apiService.DownloadAttachmentAsync(attachment.Path);
                if (stream == null)
                {
                    StatusMessage = "Download fehlgeschlagen.";
                    return;
                }

                var fileName = attachment.Name ?? "download";
                var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

                using (var fileStream = File.Create(targetPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(targetPath)
                });

                StatusMessage = "Datei geöffnet.";
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Download: {message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReloadTicketAsync()
        {
            if (Ticket == null) return;
            try
            {
                var json = await _apiService.GetTicketAsync(Ticket.Id);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                if (updated != null)
                {
                    TrudeskTranslationHelper.TranslateTicket(updated);
                    Ticket = updated;
                    EditSubject = updated.Subject ?? string.Empty;
                    EditIssue = updated.Issue ?? string.Empty;
                    SelectedStatus = Statuses.FirstOrDefault(s => s.Id == updated.Status?.Id);
                    SelectedPriority = Priorities.FirstOrDefault(p => p.Id == updated.Priority?.Id);
                    THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                }
            }
            catch { }
        }
    }
}

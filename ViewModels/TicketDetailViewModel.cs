using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;
using THWTicketApp.Data;
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

        [ObservableProperty]
        private ObservableCollection<string> _quickReplyTemplates = [];

        [ObservableProperty]
        private string? _selectedQuickReply;

        [ObservableProperty]
        private string _newTemplateName = string.Empty;

        [ObservableProperty]
        private bool _isTemplateEditorVisible;

        // Time tracking
        [ObservableProperty]
        private bool _isTimerRunning;

        [ObservableProperty]
        private string _timerDisplay = "00:00:00";

        [ObservableProperty]
        private string _totalTimeDisplay = "0h 0m";

        [ObservableProperty]
        private ObservableCollection<TimeEntry> _timeEntries = [];

        [ObservableProperty]
        private string _timeEntryDescription = string.Empty;

        private int? _activeTimerEntryId;
        private IDispatcherTimer? _timerTick;

        // Linked Tickets
        [ObservableProperty]
        private ObservableCollection<LinkedTicket> _linkedTickets = [];

        [ObservableProperty]
        private bool _isLinkPickerVisible;

        [ObservableProperty]
        private ObservableCollection<Models.Ticket> _availableTicketsForLink = [];

        [ObservableProperty]
        private Models.Ticket? _selectedTicketToLink;

        [ObservableProperty]
        private string _selectedLinkType = "related";

        // @Mentions
        [ObservableProperty]
        private ObservableCollection<User> _mentionSuggestions = [];

        [ObservableProperty]
        private bool _isMentionPopupVisible;

        // Subscription
        [ObservableProperty]
        private bool _isSubscribed;

        private readonly ITrueDeskApiService _apiService;
        private readonly ISyncService _syncService;
        private readonly IDatabaseService _databaseService;

        private static readonly string[] DefaultTemplates =
        [
            "Vielen Dank für Ihre Anfrage. Wir bearbeiten Ihr Ticket.",
            "Das Problem wurde behoben. Bitte bestätigen Sie.",
            "Könnten Sie weitere Details bereitstellen?",
            "Das Ticket wurde an die zuständige Abteilung weitergeleitet.",
            "Wir benötigen Ihre Rückmeldung, um fortzufahren."
        ];

        public TicketDetailViewModel(ITrueDeskApiService apiService, ISyncService syncService, IDatabaseService databaseService)
        {
            _apiService = apiService;
            _syncService = syncService;
            _databaseService = databaseService;
            _syncService.PendingCountChanged += count =>
                MainThread.BeginInvokeOnMainThread(() => PendingActionsCount = count);
            LoadQuickReplyTemplates();
        }

        private void LoadQuickReplyTemplates()
        {
            var saved = Preferences.Get("QuickReplyTemplates", string.Empty);
            QuickReplyTemplates.Clear();

            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    var templates = System.Text.Json.JsonSerializer.Deserialize<string[]>(saved);
                    if (templates != null)
                        foreach (var t in templates) QuickReplyTemplates.Add(t);
                }
                catch { }
            }

            if (QuickReplyTemplates.Count == 0)
            {
                foreach (var t in DefaultTemplates)
                    QuickReplyTemplates.Add(t);
                SaveTemplates();
            }
        }

        private void SaveTemplates()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(QuickReplyTemplates.ToArray());
            Preferences.Set("QuickReplyTemplates", json);
        }

        partial void OnSelectedQuickReplyChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                NewComment = value;
                SelectedQuickReply = null;
            }
        }

        partial void OnNewCommentChanged(string value)
        {
            CheckForMentions(value);
        }

        private void CheckForMentions(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                IsMentionPopupVisible = false;
                return;
            }

            // Find the last @ in the text
            var lastAtIndex = text.LastIndexOf('@');
            if (lastAtIndex < 0 || lastAtIndex == text.Length - 1)
            {
                IsMentionPopupVisible = false;
                return;
            }

            // Check if there's a space after the @, if so we're not in a mention anymore
            var afterAt = text[(lastAtIndex + 1)..];
            if (afterAt.Contains(' '))
            {
                IsMentionPopupVisible = false;
                return;
            }

            // Filter users by the text after @
            var query = afterAt.ToLowerInvariant();
            var matches = Users.Where(u =>
                (u.Fullname?.ToLowerInvariant().Contains(query) ?? false) ||
                (u.Username?.ToLowerInvariant().Contains(query) ?? false))
                .Take(5)
                .ToList();

            MentionSuggestions.Clear();
            foreach (var m in matches) MentionSuggestions.Add(m);
            IsMentionPopupVisible = matches.Count > 0;
        }

        [RelayCommand]
        private void InsertMention(User user)
        {
            if (user == null || string.IsNullOrEmpty(NewComment)) return;

            var lastAtIndex = NewComment.LastIndexOf('@');
            if (lastAtIndex >= 0)
            {
                NewComment = NewComment[..(lastAtIndex)] + $"@{user.Username} ";
            }
            IsMentionPopupVisible = false;
        }

        public void SetTicket(Ticket ticket)
        {
            TrudeskTranslationHelper.TranslateTicket(ticket);
            Ticket = ticket;
            EditSubject = ticket?.Subject ?? string.Empty;
            EditIssue = ticket?.Issue ?? string.Empty;
            // Check if current user is subscribed
            var userId = _apiService.CurrentUserId;
            IsSubscribed = ticket?.Subscribers?.Contains(userId ?? "") == true;
            _ = LoadTimeTrackingAsync();
            _ = LoadLinkedTicketsAsync();
        }

        private async Task LoadLinkedTicketsAsync()
        {
            if (Ticket == null) return;
            var links = await _databaseService.GetLinkedTicketsAsync(Ticket.Id);
            LinkedTickets.Clear();
            foreach (var l in links) LinkedTickets.Add(l);
        }

        [RelayCommand]
        private async Task ToggleLinkPickerAsync()
        {
            IsLinkPickerVisible = !IsLinkPickerVisible;
            if (IsLinkPickerVisible && AvailableTicketsForLink.Count == 0)
            {
                try
                {
                    var json = await _apiService.GetTicketsAsync();
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var tickets = System.Text.Json.JsonSerializer.Deserialize<Models.Ticket[]>(json, options);
                    AvailableTicketsForLink.Clear();
                    if (tickets != null)
                    {
                        foreach (var t in tickets.Where(t => t.Id != Ticket?.Id))
                            AvailableTicketsForLink.Add(t);
                    }
                }
                catch { StatusMessage = "Tickets konnten nicht geladen werden."; }
            }
        }

        [RelayCommand]
        private async Task AddLinkedTicketAsync()
        {
            if (Ticket == null || SelectedTicketToLink == null) return;

            await _databaseService.AddLinkedTicketAsync(
                Ticket.Id,
                SelectedTicketToLink.Id,
                SelectedTicketToLink.Subject ?? "",
                SelectedTicketToLink.Uid,
                SelectedLinkType);

            SelectedTicketToLink = null;
            IsLinkPickerVisible = false;
            await LoadLinkedTicketsAsync();
            StatusMessage = "Ticket verknüpft.";
        }

        [RelayCommand]
        private async Task RemoveLinkedTicketAsync(LinkedTicket link)
        {
            if (link == null) return;
            await _databaseService.RemoveLinkedTicketAsync(link.Id);
            await LoadLinkedTicketsAsync();
        }

        private async Task LoadTimeTrackingAsync()
        {
            if (Ticket == null) return;

            var activeTimer = await _databaseService.GetActiveTimerAsync(Ticket.Id);
            if (activeTimer != null)
            {
                _activeTimerEntryId = activeTimer.Id;
                IsTimerRunning = true;
                StartTimerTick(activeTimer.StartTime);
            }

            await RefreshTimeEntriesAsync();
        }

        private async Task RefreshTimeEntriesAsync()
        {
            if (Ticket == null) return;
            var entries = await _databaseService.GetTimeEntriesAsync(Ticket.Id);
            TimeEntries.Clear();
            foreach (var e in entries) TimeEntries.Add(e);

            var totalMinutes = await _databaseService.GetTotalTimeAsync(Ticket.Id);
            var hours = (int)(totalMinutes / 60);
            var minutes = (int)(totalMinutes % 60);
            TotalTimeDisplay = $"{hours}h {minutes}m";
        }

        [RelayCommand]
        private async Task ToggleTimerAsync()
        {
            if (Ticket == null) return;

            if (IsTimerRunning)
            {
                // Stop timer
                if (_activeTimerEntryId.HasValue)
                {
                    await _databaseService.StopTimerAsync(_activeTimerEntryId.Value, TimeEntryDescription);
                    TimeEntryDescription = string.Empty;
                }
                StopTimerTick();
                IsTimerRunning = false;
                _activeTimerEntryId = null;
                TimerDisplay = "00:00:00";
                await RefreshTimeEntriesAsync();
            }
            else
            {
                // Start timer
                var entry = await _databaseService.StartTimerAsync(Ticket.Id);
                _activeTimerEntryId = entry.Id;
                IsTimerRunning = true;
                StartTimerTick(entry.StartTime);
            }
        }

        [RelayCommand]
        private async Task DeleteTimeEntryAsync(TimeEntry entry)
        {
            if (entry == null) return;
            await _databaseService.DeleteTimeEntryAsync(entry.Id);
            await RefreshTimeEntriesAsync();
        }

        private void StartTimerTick(DateTime startTime)
        {
            StopTimerTick();
            _timerTick = Application.Current?.Dispatcher.CreateTimer();
            if (_timerTick == null) return;
            _timerTick.Interval = TimeSpan.FromSeconds(1);
            _timerTick.Tick += (_, _) =>
            {
                var elapsed = DateTime.UtcNow - startTime;
                TimerDisplay = elapsed.ToString(@"hh\:mm\:ss");
            };
            _timerTick.Start();
        }

        private void StopTimerTick()
        {
            _timerTick?.Stop();
            _timerTick = null;
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
                // Use dedicated assignees endpoint (only agents/admins, more efficient)
                var json = await _apiService.GetAssigneesAsync();
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
                var statusList = Utils.JsonHelper.DeserializeWrappedArray<Status>(json, "status", options);

                Statuses.Clear();
                if (statusList.Length > 0)
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
                // Use dedicated priorities endpoint instead of extracting from ticket types
                var json = await _apiService.GetPrioritiesAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Priorities.Clear();
                // Response may be wrapped: {"success":true,"priorities":[...]} or just array
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                Priority[]? priorityList = null;
                if (doc.RootElement.TryGetProperty("priorities", out var prioEl))
                    priorityList = System.Text.Json.JsonSerializer.Deserialize<Priority[]>(prioEl.GetRawText(), options);
                else
                    priorityList = System.Text.Json.JsonSerializer.Deserialize<Priority[]>(json, options);

                if (priorityList != null)
                {
                    foreach (var priority in priorityList)
                    {
                        priority.Name = TrudeskTranslationHelper.TranslatePriority(priority.Name);
                        Priorities.Add(priority);
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
        private void ToggleTemplateEditor()
        {
            IsTemplateEditorVisible = !IsTemplateEditorVisible;
        }

        [RelayCommand]
        private void AddTemplate()
        {
            if (string.IsNullOrWhiteSpace(NewTemplateName)) return;
            if (!QuickReplyTemplates.Contains(NewTemplateName))
            {
                QuickReplyTemplates.Add(NewTemplateName);
                SaveTemplates();
            }
            NewTemplateName = string.Empty;
        }

        [RelayCommand]
        private void RemoveTemplate(string template)
        {
            if (QuickReplyTemplates.Remove(template))
                SaveTemplates();
        }

        [RelayCommand]
        private async Task ChangeStatusAsync()
        {
            if (Ticket == null || SelectedStatus == null)
            {
                StatusMessage = "Bitte wählen Sie einen Status.";
                return;
            }

            if (string.IsNullOrEmpty(SelectedStatus.Id))
            {
                StatusMessage = "Ungültiger Status.";
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
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await _syncService.EnqueueUpdateStatusAsync(Ticket.Id, Ticket.Uid, SelectedStatus.Id, Ticket.Updated);
                    StatusMessage = "Offline: Status-Änderung wird bei Verbindung gesendet.";
                }
                else
                {
                    var (message, _) = Utils.ErrorHelper.Categorize(ex);
                    StatusMessage = $"Status: {message}";
                }
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
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await _syncService.EnqueueClearAssigneeAsync(Ticket.Id, Ticket.Uid, Ticket.Updated);
                    StatusMessage = "Offline: Zuweisung wird bei Verbindung entfernt.";
                }
                else
                {
                    var (message, _) = Utils.ErrorHelper.Categorize(ex);
                    StatusMessage = $"Zuweisung entfernen: {message}";
                }
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
                    await _syncService.EnqueueCommentAsync(Ticket.Id, Ticket.Uid, ownerId, NewComment, Ticket.Updated);
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
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await _syncService.EnqueueEditTicketAsync(
                        Ticket.Id, Ticket.Uid, EditSubject, EditIssue,
                        SelectedPriority?.Id, SelectedStatus?.Id, Ticket.Updated);
                    StatusMessage = "Offline: Änderung wird bei Verbindung gesendet.";
                }
                else
                {
                    var (message, _) = Utils.ErrorHelper.Categorize(ex);
                    StatusMessage = $"Bearbeitung: {message}";
                }
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
                    await _syncService.EnqueueNoteAsync(Ticket.Id, Ticket.Uid, ownerId, NewNote, Ticket.Updated);
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
        private async Task TakePhotoAndAttachAsync()
        {
            if (Ticket == null)
            {
                StatusMessage = "Kein Ticket ausgewählt.";
                return;
            }

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return;

                IsLoading = true;
                StatusMessage = "Foto wird hochgeladen...";

                using var stream = await photo.OpenReadAsync();
                var success = await _apiService.UploadAttachmentAsync(Ticket.Id, stream, photo.FileName);

                if (success)
                {
                    StatusMessage = "Foto hochgeladen.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Upload fehlgeschlagen.";
                }
            }
            catch (FeatureNotSupportedException)
            {
                StatusMessage = "Kamera nicht verfügbar auf diesem Gerät.";
            }
            catch (PermissionException)
            {
                StatusMessage = "Kamera-Berechtigung wurde verweigert.";
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Foto: {message}";
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

        [RelayCommand]
        private async Task DeleteAttachmentAsync(Models.Attachment attachment)
        {
            if (Ticket == null || string.IsNullOrEmpty(attachment?.Id)) return;

            IsLoading = true;
            StatusMessage = "Anhang wird gelöscht...";

            try
            {
                var success = await _apiService.DeleteAttachmentAsync(Ticket.Id, attachment.Id!);
                if (success)
                {
                    StatusMessage = "Anhang gelöscht.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Löschen fehlgeschlagen.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Löschen: {message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PreviewAttachmentAsync(Models.Attachment attachment)
        {
            if (attachment?.Path == null) return;

            if (attachment.IsImage)
            {
                // Download image to cache and show inline preview
                try
                {
                    var fileName = attachment.Name ?? "preview";
                    var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

                    if (!File.Exists(targetPath))
                    {
                        using var stream = await _apiService.DownloadAttachmentAsync(attachment.Path);
                        if (stream == null) return;
                        using var fileStream = File.Create(targetPath);
                        await stream.CopyToAsync(fileStream);
                    }

                    PreviewImageSource = ImageSource.FromFile(targetPath);
                    IsPreviewVisible = true;
                }
                catch { }
            }
            else
            {
                // Non-image: download and open with native viewer
                await DownloadAttachmentAsync(attachment);
            }
        }

        private ImageSource? _previewImageSource;
        public ImageSource? PreviewImageSource
        {
            get => _previewImageSource;
            set => SetProperty(ref _previewImageSource, value);
        }

        private bool _isPreviewVisible;
        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set => SetProperty(ref _isPreviewVisible, value);
        }

        [RelayCommand]
        private void ClosePreview()
        {
            IsPreviewVisible = false;
            PreviewImageSource = null;
        }

        [RelayCommand]
        private async Task ToggleSubscriptionAsync()
        {
            if (Ticket == null) return;

            IsLoading = true;
            try
            {
                var newState = !IsSubscribed;
                var success = await _apiService.SubscribeToTicketAsync(Ticket.Id, newState);
                if (success)
                {
                    IsSubscribed = newState;
                    StatusMessage = newState ? "Ticket abonniert." : "Abo beendet.";
                    await ReloadTicketAsync();
                }
                else
                {
                    StatusMessage = "Abo-Änderung fehlgeschlagen.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Abo: {message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteTicketAsync()
        {
            if (Ticket == null) return;

            IsLoading = true;
            try
            {
                var success = await _apiService.DeleteTicketAsync(Ticket.Id);
                if (success)
                {
                    StatusMessage = "Ticket gelöscht.";
                    // Navigate back
                    await Task.Delay(500);
                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window?.Page is NavigationPage nav)
                    {
                        await nav.Navigation.PopAsync();
                    }
                }
                else
                {
                    StatusMessage = "Löschen fehlgeschlagen.";
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Löschen: {message}";
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
                var json = await _apiService.GetTicketAsync(Ticket.Uid.ToString());
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                // Single ticket response is wrapped: {"success":true,"ticket":{...}}
                Ticket? updated = null;
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("ticket", out var ticketEl))
                        updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(ticketEl.GetRawText(), options);
                    else
                        updated = System.Text.Json.JsonSerializer.Deserialize<Ticket>(json, options);
                }
                if (updated != null)
                {
                    TrudeskTranslationHelper.TranslateTicket(updated);
                    Ticket = updated;
                    EditSubject = updated.Subject ?? string.Empty;
                    EditIssue = updated.Issue ?? string.Empty;
                    SelectedStatus = Statuses.FirstOrDefault(s => s.Id == updated.Status?.Id);
                    SelectedPriority = Priorities.FirstOrDefault(p => p.Id == updated.Priority?.Id);
                    var userId = _apiService.CurrentUserId;
                    IsSubscribed = updated.Subscribers?.Contains(userId ?? "") == true;
                    THWTicketApp.Utils.NotificationCenter.RaiseTicketUpdated(Ticket.Id);
                }
            }
            catch { }
        }
    }
}

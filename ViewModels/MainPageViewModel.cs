using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ITrueDeskApiService _apiService;
        private readonly IDatabaseService _databaseService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISyncService _syncService;
        private readonly RealtimeService _realtimeService;
        private List<Ticket> _allTickets = [];

        [ObservableProperty]
        private string _welcomeMessage = string.Empty;

        [ObservableProperty]
        private bool _isAuthenticated;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _totalTickets;

        [ObservableProperty]
        private int _openTickets;

        [ObservableProperty]
        private int _pendingTickets;

        [ObservableProperty]
        private int _closedTickets;

        [ObservableProperty]
        private int _myTickets;

        [ObservableProperty]
        private int _overdueTickets;

        [ObservableProperty]
        private ObservableCollection<GroupTicketCount> _ticketsPerGroup = new();

        [ObservableProperty]
        private ObservableCollection<Ticket> _recentTickets = new();

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingActions))]
        private int _pendingActionsCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUnreadNotifications))]
        private int _unreadNotificationCount;

        public bool HasUnreadNotifications => UnreadNotificationCount > 0;

        public bool HasPendingActions => PendingActionsCount > 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasConflicts))]
        private int _conflictCount;

        public bool HasConflicts => ConflictCount > 0;

        public MainPageViewModel(ITrueDeskApiService apiService, IDatabaseService databaseService, IServiceProvider serviceProvider, ISyncService syncService, RealtimeService realtimeService, NotificationService notificationService)
        {
            _apiService = apiService;
            _databaseService = databaseService;
            _serviceProvider = serviceProvider;
            _syncService = syncService;
            _realtimeService = realtimeService;
            IsAuthenticated = _apiService.IsAuthenticated;
            UpdateWelcomeMessage();

            _syncService.PendingCountChanged += count =>
                MainThread.BeginInvokeOnMainThread(() => PendingActionsCount = count);

            _realtimeService.TicketUpdated += _ => OnRealtimeUpdate();
            _realtimeService.TicketCreated += _ => OnRealtimeUpdate();

            notificationService.NotificationReceived += () =>
                MainThread.BeginInvokeOnMainThread(async () =>
                    UnreadNotificationCount = await _databaseService.GetUnreadNotificationCountAsync());

            _syncService.ConflictDetected += _ =>
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var conflicts = await _syncService.GetConflictedActionsAsync();
                    ConflictCount = conflicts.Count;
                });
        }

        private async void OnRealtimeUpdate()
        {
            if (!IsLoading)
            {
                await LoadDashboardAsync();
            }
        }

        private void UpdateWelcomeMessage()
        {
            var username = _apiService.CurrentUsername;
            WelcomeMessage = string.IsNullOrEmpty(username)
                ? "Willkommen!"
                : $"Willkommen, {username}!";
        }

        [RelayCommand]
        public async Task LoadDashboardAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            // Background tasks: sync, notifications, realtime - don't block dashboard
            _ = Task.Run(async () =>
            {
                try
                {
                    await _syncService.SyncPendingActionsAsync();
                    var count = await _syncService.GetPendingCountAsync();
                    // Use server-side notification count
                    var serverNotifCount = await _apiService.GetNotificationCountAsync();
                    var localUnread = await _databaseService.GetUnreadNotificationCountAsync();
                    var conf = await _syncService.GetConflictedActionsAsync();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PendingActionsCount = count;
                        UnreadNotificationCount = Math.Max(serverNotifCount, localUnread);
                        ConflictCount = conf.Count;
                    });
                }
                catch { }
            });
            _ = _realtimeService.ConnectAsync();

            try
            {
                var json = await _apiService.GetTicketsAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var tickets = JsonSerializer.Deserialize<Ticket[]>(json, options);

                if (tickets != null)
                {
                    _allTickets = tickets.ToList();

                    foreach (var ticket in _allTickets)
                    {
                        TrudeskTranslationHelper.TranslateTicket(ticket);
                    }

                    ComputeStatistics();
                    LoadRecentTickets();

                    // Load overdue count from server (more accurate than client-side date comparison)
                    _ = LoadOverdueCountAsync();
                }
            }
            catch (Exception ex)
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);

                // Try cache
                var isOfflineCacheEnabled = Preferences.Get("OfflineCache", false);
                if (isOfflineCacheEnabled)
                {
                    try
                    {
                        var cached = await _databaseService.GetTicketsFromCacheAsync();
                        if (cached.Count > 0)
                        {
                            _allTickets = cached;
                            ComputeStatistics();
                            LoadRecentTickets();
                            HasError = true;
                            ErrorMessage = $"Offline-Modus: {message}";
                            return;
                        }
                    }
                    catch { }
                }

                HasError = true;
                ErrorMessage = message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshDashboardAsync()
        {
            IsRefreshing = true;
            await LoadDashboardAsync();
            IsRefreshing = false;
        }

        private void ComputeStatistics()
        {
            TotalTickets = _allTickets.Count;

            OpenTickets = _allTickets.Count(t =>
                t.Status?.Name?.ToLowerInvariant() is "offen" or "open" or "neu" or "new");

            PendingTickets = _allTickets.Count(t =>
                t.Status?.Name?.ToLowerInvariant() is "ausstehend" or "pending" or "in bearbeitung" or "in progress" or "wartend" or "on hold");

            ClosedTickets = _allTickets.Count(t =>
                t.Status?.IsResolved == true ||
                t.Status?.Name?.ToLowerInvariant() is "geschlossen" or "closed" or "gelöst" or "resolved");

            var username = _apiService.CurrentUsername;
            if (!string.IsNullOrEmpty(username))
            {
                MyTickets = _allTickets.Count(t =>
                    t.Assignee?.Username?.Equals(username, StringComparison.OrdinalIgnoreCase) == true);
            }

            OverdueTickets = _allTickets.Count(t =>
                t.DueDate != DateTime.MinValue &&
                t.DueDate < DateTime.Now &&
                t.Status?.IsResolved != true);

            // Tickets per group
            TicketsPerGroup.Clear();
            var groups = _allTickets
                .Where(t => t.Group != null && !string.IsNullOrEmpty(t.Group.Name))
                .GroupBy(t => t.Group!.Name)
                .OrderByDescending(g => g.Count());
            foreach (var g in groups)
            {
                var openCount = g.Count(t => t.Status?.IsResolved != true);
                TicketsPerGroup.Add(new GroupTicketCount(g.Key!, g.Count(), openCount));
            }
        }

        private async Task LoadOverdueCountAsync()
        {
            try
            {
                var json = await _apiService.GetOverdueTicketsAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                int count = 0;
                if (doc.RootElement.TryGetProperty("tickets", out var ticketsEl) && ticketsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    count = ticketsEl.GetArrayLength();
                else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    count = doc.RootElement.GetArrayLength();
                OverdueTickets = count;
            }
            catch { /* Keep client-side calculated value */ }
        }

        private void LoadRecentTickets()
        {
            RecentTickets.Clear();
            var recent = _allTickets
                .OrderByDescending(t => t.Updated)
                .Take(5);
            foreach (var ticket in recent)
            {
                RecentTickets.Add(ticket);
            }
        }

        [RelayCommand]
        private async Task NavigateToTicketsAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var ticketPage = _serviceProvider.GetRequiredService<TicketPage>();
                await nav.PushAsync(ticketPage);
            }
        }

        [RelayCommand]
        private async Task NavigateToAddTicketAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var addTicketPage = _serviceProvider.GetRequiredService<Views.AddTicketPage>();
                await nav.PushAsync(addTicketPage);
            }
        }

        [RelayCommand]
        private async Task NavigateToTicketDetailAsync(Ticket ticket)
        {
            if (ticket == null) return;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var detailPage = _serviceProvider.GetRequiredService<Views.TicketDetailPage>();
                detailPage.SetTicket(ticket);
                await nav.PushAsync(detailPage);
            }
        }

        [RelayCommand]
        private async Task NavigateToTeamDashboardAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var teamPage = _serviceProvider.GetRequiredService<Views.TeamDashboardPage>();
                await nav.PushAsync(teamPage);
            }
        }

        [RelayCommand]
        private async Task NavigateToKanbanAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var kanbanPage = _serviceProvider.GetRequiredService<Views.KanbanBoardPage>();
                await nav.PushAsync(kanbanPage);
            }
        }

        [RelayCommand]
        private async Task NavigateToReportingAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var page = _serviceProvider.GetRequiredService<Views.ReportingPage>();
                await nav.PushAsync(page);
            }
        }

        [RelayCommand]
        private async Task NavigateToConflictsAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var page = _serviceProvider.GetRequiredService<Views.SyncConflictPage>();
                await nav.PushAsync(page);
            }
        }

        [RelayCommand]
        private async Task NavigateToNotificationsAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var page = _serviceProvider.GetRequiredService<Views.NotificationHistoryPage>();
                await nav.PushAsync(page);
            }
        }

        [RelayCommand]
        private async Task QuickAssignAsync(Ticket ticket)
        {
            if (ticket == null) return;
            var userId = _apiService.CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                var success = await _apiService.AssignTicketAsync(ticket.Id, userId);
                if (success)
                    await LoadDashboardAsync();
            }
            catch { }
        }

        [RelayCommand]
        private async Task QuickCloseAsync(Ticket ticket)
        {
            if (ticket == null) return;

            try
            {
                // Get closed status
                var statusJson = await _apiService.GetStatusesAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var statuses = Utils.JsonHelper.DeserializeWrappedArray<Status>(statusJson, "status", options);
                var closedStatus = statuses.FirstOrDefault(s => s.IsResolved);

                if (closedStatus != null)
                {
                    var success = await _apiService.UpdateTicketStatusAsync(ticket.Id, closedStatus.Id!);
                    if (success)
                        await LoadDashboardAsync();
                }
            }
            catch { }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _apiService.LogoutAsync();
            IsAuthenticated = false;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                await nav.PopToRootAsync();
            }
        }
    }

    public record GroupTicketCount(string GroupName, int Total, int Open);
}

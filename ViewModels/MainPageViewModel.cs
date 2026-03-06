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
        private readonly TrueDeskApiService _apiService;
        private readonly DatabaseService _databaseService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SyncService _syncService;
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
        private ObservableCollection<Ticket> _recentTickets = new();

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingActions))]
        private int _pendingActionsCount;

        public bool HasPendingActions => PendingActionsCount > 0;

        public MainPageViewModel(TrueDeskApiService apiService, DatabaseService databaseService, IServiceProvider serviceProvider, SyncService syncService, RealtimeService realtimeService)
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

            // Sync any queued offline actions
            await _syncService.SyncPendingActionsAsync();
            PendingActionsCount = await _syncService.GetPendingCountAsync();

            // Connect to real-time updates
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
        private async Task LogoutAsync()
        {
            _apiService.Logout();
            IsAuthenticated = false;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                await nav.PopToRootAsync();
            }
        }
    }
}

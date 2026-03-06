using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class TicketPageViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseService _databaseService;
    private readonly SyncService _syncService;
    private readonly RealtimeService _realtimeService;
    private List<Ticket> _allTickets = [];
    private bool _isOfflineCacheEnabled;

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private ObservableCollection<Ticket> _tickets = new();
    public ObservableCollection<Ticket> Tickets
    {
        get => _tickets;
        set => SetProperty(ref _tickets, value);
    }

    private ObservableCollection<Ticket> _filteredTickets = new();
    public ObservableCollection<Ticket> FilteredTickets
    {
        get => _filteredTickets;
        set => SetProperty(ref _filteredTickets, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _activeFilter = "all";
    public string ActiveFilter
    {
        get => _activeFilter;
        set => SetProperty(ref _activeFilter, value);
    }

    private string _activeSort = "date_desc";
    public string ActiveSort
    {
        get => _activeSort;
        set => SetProperty(ref _activeSort, value);
    }

    private bool _isOfflineMode;
    public bool IsOfflineMode
    {
        get => _isOfflineMode;
        set => SetProperty(ref _isOfflineMode, value);
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    private bool _canRetry;
    public bool CanRetry
    {
        get => _canRetry;
        set => SetProperty(ref _canRetry, value);
    }

    private int _pendingActionsCount;
    public int PendingActionsCount
    {
        get => _pendingActionsCount;
        set
        {
            if (SetProperty(ref _pendingActionsCount, value))
                OnPropertyChanged(nameof(HasPendingActions));
        }
    }

    public bool HasPendingActions => PendingActionsCount > 0;

    private bool _isRealtimeConnected;
    public bool IsRealtimeConnected
    {
        get => _isRealtimeConnected;
        set => SetProperty(ref _isRealtimeConnected, value);
    }

    public TicketPageViewModel(TrueDeskApiService apiService, IServiceProvider serviceProvider, DatabaseService databaseService, SyncService syncService, RealtimeService realtimeService)
    {
        _apiService = apiService;
        _serviceProvider = serviceProvider;
        _databaseService = databaseService;
        _syncService = syncService;
        _realtimeService = realtimeService;
        _isOfflineCacheEnabled = Preferences.Get("OfflineCache", false);

        _syncService.PendingCountChanged += count =>
            MainThread.BeginInvokeOnMainThread(() => PendingActionsCount = count);

        _realtimeService.TicketUpdated += _ => OnRealtimeUpdate();
        _realtimeService.TicketCreated += _ => OnRealtimeUpdate();
        _realtimeService.CommentAdded += _ => OnRealtimeUpdate();
    }

    private async void OnRealtimeUpdate()
    {
        if (!IsLoading && !IsRefreshing)
        {
            await LoadTicketsAsync();
        }
    }

    [RelayCommand]
    private void Search()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void Filter(string filterType)
    {
        ActiveFilter = filterType;
        ApplyFilters();
    }

    [RelayCommand]
    private void Sort(string sortType)
    {
        ActiveSort = sortType;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allTickets.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Subject?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Issue?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Owner?.Fullname?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Assignee?.Fullname?.ToLowerInvariant().Contains(search) ?? false)
            );
        }

        // Apply status filter (check both German translated and English original names)
        filtered = ActiveFilter switch
        {
            "open" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() is "offen" or "open" or "neu" or "new"),
            "pending" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() is "ausstehend" or "pending" or "in bearbeitung" or "in progress" or "wartend" or "on hold"),
            "closed" => filtered.Where(t => t.Status?.IsResolved == true ||
                                            t.Status?.Name?.ToLowerInvariant() is "geschlossen" or "closed" or "gelöst" or "resolved"),
            _ => filtered
        };

        // Apply sorting
        var sorted = ActiveSort switch
        {
            "date_asc" => filtered.OrderBy(t => t.Date),
            "date_desc" => filtered.OrderByDescending(t => t.Date),
            "updated" => filtered.OrderByDescending(t => t.Updated),
            "priority" => filtered.OrderByDescending(t => t.Priority?.OverdueIn ?? 0),
            "subject" => filtered.OrderBy(t => t.Subject, StringComparer.OrdinalIgnoreCase),
            "duedate" => filtered.OrderBy(t => t.DueDate == DateTime.MinValue ? DateTime.MaxValue : t.DueDate),
            _ => filtered.OrderByDescending(t => t.Date)
        };

        FilteredTickets.Clear();
        foreach (var ticket in sorted)
        {
            FilteredTickets.Add(ticket);
        }

        OnPropertyChanged(nameof(HasStatusMessage));
    }

    [RelayCommand]
    public async Task LoadTicketsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        IsOfflineMode = false;
        StatusMessage = "Lade Tickets...";
        OnPropertyChanged(nameof(HasStatusMessage));

        // Sync any queued offline actions first
        await _syncService.SyncPendingActionsAsync();
        PendingActionsCount = await _syncService.GetPendingCountAsync();

        // Connect to real-time updates
        _ = ConnectRealtimeAsync();

        try
        {
            var json = await _apiService.GetTicketsAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var tickets = JsonSerializer.Deserialize<Ticket[]>(json, options);

            _allTickets.Clear();
            Tickets.Clear();

            if (tickets == null || tickets.Length == 0)
            {
                StatusMessage = "Keine Tickets gefunden.";
            }
            else
            {
                foreach (var ticket in tickets)
                {
                    TrudeskTranslationHelper.TranslateTicket(ticket);
                    _allTickets.Add(ticket);
                    Tickets.Add(ticket);
                }
                StatusMessage = string.Empty;

                // Save to cache if enabled
                if (_isOfflineCacheEnabled)
                {
                    await _databaseService.SaveTicketsAsync(tickets);
                }
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            var (message, canRetry) = Utils.ErrorHelper.Categorize(ex);
            CanRetry = canRetry;
            await LoadFromCacheAsync(message);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    private async Task LoadFromCacheAsync(string message)
    {
        if (!_isOfflineCacheEnabled)
        {
            StatusMessage = "Verbindungsfehler. Offline-Cache ist deaktiviert.";
            return;
        }

        try
        {
            var cachedTickets = await _databaseService.GetTicketsFromCacheAsync();

            if (cachedTickets.Count == 0)
            {
                StatusMessage = "Keine gecachten Daten verfügbar.";
                return;
            }

            _allTickets.Clear();
            Tickets.Clear();

            foreach (var ticket in cachedTickets)
            {
                _allTickets.Add(ticket);
                Tickets.Add(ticket);
            }

            IsOfflineMode = true;
            var lastCache = await _databaseService.GetLastCacheTimeAsync();
            var cacheInfo = lastCache.HasValue
                ? $" (Stand: {lastCache.Value:dd.MM.yyyy HH:mm})"
                : "";

            StatusMessage = $"{message}{cacheInfo}";
            ApplyFilters();
        }
        catch
        {
            StatusMessage = "Fehler beim Laden des Cache.";
        }
    }

    [RelayCommand]
    public async Task RefreshTicketsAsync()
    {
        IsRefreshing = true;
        await LoadTicketsAsync();
    }

    [RelayCommand]
    private async Task ShowAddTicketAsync()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is NavigationPage nav)
        {
            var addTicketPage = _serviceProvider.GetRequiredService<Views.AddTicketPage>();
            await nav.PushAsync(addTicketPage);
        }
    }

    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is NavigationPage nav)
        {
            var settingsPage = _serviceProvider.GetRequiredService<Views.SettingsPage>();
            await nav.PushAsync(settingsPage);
        }
    }

    [RelayCommand]
    private async Task AssignToMeAsync(Ticket ticket)
    {
        if (ticket == null) return;

        var userId = _apiService.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            StatusMessage = "Benutzer-ID nicht verfügbar.";
            OnPropertyChanged(nameof(HasStatusMessage));
            return;
        }

        try
        {
            var success = await _apiService.AssignTicketAsync(ticket.Id, userId);
            if (success)
            {
                StatusMessage = "Ticket dir zugewiesen.";
                CanRetry = false;
                await LoadTicketsAsync();
            }
            else
            {
                StatusMessage = "Zuweisung fehlgeschlagen.";
            }
        }
        catch (Exception ex)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await _syncService.EnqueueAssignAsync(ticket.Id, userId);
                StatusMessage = "Offline: Zuweisung wird bei Verbindung gesendet.";
            }
            else
            {
                var (message, _) = Utils.ErrorHelper.Categorize(ex);
                StatusMessage = $"Zuweisung: {message}";
            }
        }
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    [RelayCommand]
    private async Task CloseTicketAsync(Ticket ticket)
    {
        if (ticket == null) return;

        try
        {
            // Find the "closed/resolved" status - try to use the ticket's existing statuses
            // Set status to resolved by updating the ticket
            var editTicket = new Ticket
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Issue = ticket.Issue,
                Priority = ticket.Priority,
                Status = new Status { Id = ticket.Status?.Id, Name = "Closed", IsResolved = true }
            };

            // First try to get proper closed status from API
            try
            {
                var statusJson = await _apiService.GetStatusesAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var statuses = System.Text.Json.JsonSerializer.Deserialize<Status[]>(statusJson, options);
                var closedStatus = statuses?.FirstOrDefault(s => s.IsResolved);
                if (closedStatus != null)
                {
                    editTicket.Status = closedStatus;
                }
            }
            catch { }

            var success = await _apiService.EditTicketAsync(editTicket);
            if (success)
            {
                StatusMessage = "Ticket geschlossen.";
                await LoadTicketsAsync();
            }
            else
            {
                StatusMessage = "Schließen fehlgeschlagen.";
            }
        }
        catch (Exception ex)
        {
            var (message, _) = Utils.ErrorHelper.Categorize(ex);
            StatusMessage = $"Schließen: {message}";
        }
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private async Task ConnectRealtimeAsync()
    {
        try
        {
            await _realtimeService.ConnectAsync();
            IsRealtimeConnected = _realtimeService.IsConnected;
        }
        catch
        {
            IsRealtimeConnected = false;
        }
    }

    public async Task AddTicketAsync(string title, string description, int assignedUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            StatusMessage = "Titel ist erforderlich.";
            OnPropertyChanged(nameof(HasStatusMessage));
            return;
        }

        IsLoading = true;
        try
        {
            await _apiService.AddTicketAsync(title, description, assignedUserId);
            StatusMessage = "Ticket erstellt.";
            await LoadTicketsAsync();
        }
        catch (Exception)
        {
            StatusMessage = "Fehler beim Erstellen des Tickets.";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }
}

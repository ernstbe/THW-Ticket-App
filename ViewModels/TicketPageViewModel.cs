using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Data;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class TicketPageViewModel : ObservableObject
{
    private readonly ITrueDeskApiService _apiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatabaseService _databaseService;
    private readonly ISyncService _syncService;
    private readonly RealtimeService _realtimeService;
    private List<Ticket> _allTickets = [];
    private bool _isOfflineCacheEnabled;
    private CancellationTokenSource? _searchCts;
    private bool _isServerSearchActive;

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
                _ = DebouncedSearchAsync(value);
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

    // Advanced Filter properties
    private bool _isFilterPanelVisible;
    public bool IsFilterPanelVisible
    {
        get => _isFilterPanelVisible;
        set => SetProperty(ref _isFilterPanelVisible, value);
    }

    private ObservableCollection<Priority> _availablePriorities = new();
    public ObservableCollection<Priority> AvailablePriorities
    {
        get => _availablePriorities;
        set => SetProperty(ref _availablePriorities, value);
    }

    private Priority? _selectedPriorityFilter;
    public Priority? SelectedPriorityFilter
    {
        get => _selectedPriorityFilter;
        set { if (SetProperty(ref _selectedPriorityFilter, value)) ApplyFilters(); }
    }

    private ObservableCollection<TicketType> _availableTypes = new();
    public ObservableCollection<TicketType> AvailableTypes
    {
        get => _availableTypes;
        set => SetProperty(ref _availableTypes, value);
    }

    private TicketType? _selectedTypeFilter;
    public TicketType? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set { if (SetProperty(ref _selectedTypeFilter, value)) ApplyFilters(); }
    }

    private ObservableCollection<Group> _availableGroups = new();
    public ObservableCollection<Group> AvailableGroups
    {
        get => _availableGroups;
        set => SetProperty(ref _availableGroups, value);
    }

    private Group? _selectedGroupFilter;
    public Group? SelectedGroupFilter
    {
        get => _selectedGroupFilter;
        set { if (SetProperty(ref _selectedGroupFilter, value)) ApplyFilters(); }
    }

    private bool _showOnlyMyTickets;
    public bool ShowOnlyMyTickets
    {
        get => _showOnlyMyTickets;
        set { if (SetProperty(ref _showOnlyMyTickets, value)) ApplyFilters(); }
    }

    private DateTime? _dateFrom;
    public DateTime? DateFrom
    {
        get => _dateFrom;
        set { if (SetProperty(ref _dateFrom, value)) ApplyFilters(); }
    }

    private DateTime? _dateTo;
    public DateTime? DateTo
    {
        get => _dateTo;
        set { if (SetProperty(ref _dateTo, value)) ApplyFilters(); }
    }

    private int _activeFilterCount;
    public int ActiveFilterCount
    {
        get => _activeFilterCount;
        set
        {
            if (SetProperty(ref _activeFilterCount, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool HasActiveFilters => ActiveFilterCount > 0;

    private bool _showOnlyFavorites;
    public bool ShowOnlyFavorites
    {
        get => _showOnlyFavorites;
        set { if (SetProperty(ref _showOnlyFavorites, value)) ApplyFilters(); }
    }

    private bool _showOnlyOverdue;
    public bool ShowOnlyOverdue
    {
        get => _showOnlyOverdue;
        set { if (SetProperty(ref _showOnlyOverdue, value)) ApplyFilters(); }
    }

    private HashSet<string> _favoriteIds = new();

    private ObservableCollection<SavedFilter> _savedFilters = new();
    public ObservableCollection<SavedFilter> SavedFilters
    {
        get => _savedFilters;
        set => SetProperty(ref _savedFilters, value);
    }

    // Bulk selection
    private bool _isBulkSelectMode;
    public bool IsBulkSelectMode
    {
        get => _isBulkSelectMode;
        set
        {
            if (SetProperty(ref _isBulkSelectMode, value))
            {
                if (!value) SelectedTicketIds.Clear();
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelectedTickets));
            }
        }
    }

    public ObservableCollection<string> SelectedTicketIds { get; } = new();
    public int SelectedCount => SelectedTicketIds.Count;
    public bool HasSelectedTickets => SelectedTicketIds.Count > 0;

    public TicketPageViewModel(ITrueDeskApiService apiService, IServiceProvider serviceProvider, IDatabaseService databaseService, ISyncService syncService, RealtimeService realtimeService)
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
        _ = DebouncedSearchAsync(SearchText, immediate: true);
    }

    private async Task DebouncedSearchAsync(string query, bool immediate = false)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (!immediate)
        {
            try { await Task.Delay(400, token); }
            catch (TaskCanceledException) { return; }
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            // Search cleared — revert to full local list
            if (_isServerSearchActive)
            {
                _isServerSearchActive = false;
                await LoadTicketsAsync();
            }
            else
            {
                ApplyFilters();
            }
            return;
        }

        // Only use server search for queries with 2+ characters
        if (query.Length < 2)
        {
            ApplyFilters();
            return;
        }

        try
        {
            var json = await _apiService.SearchTicketsAsync(query);
            if (token.IsCancellationRequested) return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<Models.Responses.GetTicketsResponse>(json, options);
            var tickets = response?.Tickets;

            if (tickets == null || tickets.Count == 0)
            {
                // Server search returned nothing — fall back to client-side filter
                _isServerSearchActive = false;
                ApplyFilters();
                return;
            }

            _isServerSearchActive = true;
            foreach (var ticket in tickets)
                TrudeskTranslationHelper.TranslateTicket(ticket);

            // Replace filtered view with server results, still apply local filters on top
            FilteredTickets.Clear();
            var filtered = ApplyLocalFilters(tickets);
            foreach (var ticket in ApplySorting(filtered))
                FilteredTickets.Add(ticket);

            UpdateActiveFilterCount();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
        catch
        {
            // Server search failed — fall back to client-side filter
            _isServerSearchActive = false;
            ApplyFilters();
        }
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

    [RelayCommand]
    private void ToggleFavoritesFilter()
    {
        ShowOnlyFavorites = !ShowOnlyFavorites;
    }

    [RelayCommand]
    private async Task ToggleOverdueFilterAsync()
    {
        ShowOnlyOverdue = !ShowOnlyOverdue;
        if (ShowOnlyOverdue)
        {
            // Load overdue tickets from server
            await LoadOverdueTicketsFromServerAsync();
        }
        else
        {
            ApplyFilters();
        }
    }

    private async Task LoadOverdueTicketsFromServerAsync()
    {
        try
        {
            var json = await _apiService.GetOverdueTicketsAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(json);
            Ticket[]? overdueTickets = null;
            if (doc.RootElement.TryGetProperty("tickets", out var ticketsEl))
                overdueTickets = JsonSerializer.Deserialize<Ticket[]>(ticketsEl.GetRawText(), options);
            else
                overdueTickets = JsonSerializer.Deserialize<Ticket[]>(json, options);

            if (overdueTickets != null && overdueTickets.Length > 0)
            {
                foreach (var t in overdueTickets)
                    TrudeskTranslationHelper.TranslateTicket(t);

                FilteredTickets.Clear();
                var sorted = ApplySorting(overdueTickets);
                foreach (var ticket in sorted)
                    FilteredTickets.Add(ticket);
                UpdateActiveFilterCount();
                return;
            }
        }
        catch { }
        // Fallback to client-side filtering
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(Ticket ticket)
    {
        if (ticket == null) return;
        await _databaseService.ToggleFavoriteAsync(ticket.Id);
        _favoriteIds = await _databaseService.GetFavoriteIdsAsync();
        ApplyFilters();
    }

    public bool IsFavorite(string ticketId) => _favoriteIds.Contains(ticketId);

    [RelayCommand]
    private void ToggleFilterPanel()
    {
        IsFilterPanelVisible = !IsFilterPanelVisible;
        if (IsFilterPanelVisible && AvailablePriorities.Count == 0)
        {
            _ = LoadFilterOptionsAsync();
        }
    }

    [RelayCommand]
    private void ClearAllFilters()
    {
        _selectedPriorityFilter = null;
        OnPropertyChanged(nameof(SelectedPriorityFilter));
        _selectedTypeFilter = null;
        OnPropertyChanged(nameof(SelectedTypeFilter));
        _selectedGroupFilter = null;
        OnPropertyChanged(nameof(SelectedGroupFilter));
        _showOnlyMyTickets = false;
        OnPropertyChanged(nameof(ShowOnlyMyTickets));
        _showOnlyFavorites = false;
        OnPropertyChanged(nameof(ShowOnlyFavorites));
        _showOnlyOverdue = false;
        OnPropertyChanged(nameof(ShowOnlyOverdue));
        _dateFrom = null;
        OnPropertyChanged(nameof(DateFrom));
        _dateTo = null;
        OnPropertyChanged(nameof(DateTo));
        ActiveFilter = "all";
        ApplyFilters();
    }

    [RelayCommand]
    private async Task SaveCurrentFilterAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return;

        var name = await page.DisplayPromptAsync("Filter speichern", "Name für den Filter:", "Speichern", "Abbrechen", placeholder: "z.B. Meine offenen Tickets");
        if (string.IsNullOrWhiteSpace(name)) return;

        var filterState = new
        {
            ActiveFilter,
            PriorityId = SelectedPriorityFilter?.Id,
            TypeId = SelectedTypeFilter?.Id,
            GroupId = SelectedGroupFilter?.Id,
            ShowOnlyMyTickets,
            ShowOnlyFavorites,
            ShowOnlyOverdue,
            DateFrom,
            DateTo,
            ActiveSort
        };

        var json = System.Text.Json.JsonSerializer.Serialize(filterState);
        var filter = new SavedFilter
        {
            Name = name,
            FilterJson = json,
            CreatedAt = DateTime.UtcNow
        };

        await _databaseService.SaveFilterAsync(filter);
        await LoadSavedFiltersAsync();
        StatusMessage = $"Filter \"{name}\" gespeichert.";
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    [RelayCommand]
    private async Task ApplySavedFilterAsync(SavedFilter filter)
    {
        if (filter == null) return;

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = System.Text.Json.JsonDocument.Parse(filter.FilterJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("ActiveFilter", out var af))
                ActiveFilter = af.GetString() ?? "all";
            if (root.TryGetProperty("ShowOnlyMyTickets", out var my))
                ShowOnlyMyTickets = my.GetBoolean();
            if (root.TryGetProperty("ShowOnlyFavorites", out var fav))
                ShowOnlyFavorites = fav.GetBoolean();
            if (root.TryGetProperty("ShowOnlyOverdue", out var od))
                ShowOnlyOverdue = od.GetBoolean();
            if (root.TryGetProperty("ActiveSort", out var sort))
                ActiveSort = sort.GetString() ?? "date_desc";

            // Re-apply filters
            ApplyFilters();
            StatusMessage = $"Filter \"{filter.Name}\" angewendet.";
            OnPropertyChanged(nameof(HasStatusMessage));
        }
        catch { }
    }

    [RelayCommand]
    private async Task DeleteSavedFilterAsync(SavedFilter filter)
    {
        if (filter == null) return;
        await _databaseService.DeleteSavedFilterAsync(filter.Id);
        await LoadSavedFiltersAsync();
    }

    private async Task LoadSavedFiltersAsync()
    {
        var filters = await _databaseService.GetSavedFiltersAsync();
        SavedFilters.Clear();
        foreach (var f in filters) SavedFilters.Add(f);
    }

    private async Task LoadFilterOptionsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Load types, priorities, and groups in parallel
            var typesTask = _apiService.GetTicketTypesAsync();
            var prioritiesTask = _apiService.GetPrioritiesAsync();
            var groupsTask = _apiService.GetGroupsAsync();
            await Task.WhenAll(typesTask, prioritiesTask, groupsTask);

            // Types
            var types = JsonSerializer.Deserialize<TicketType[]>(typesTask.Result, options);
            AvailableTypes.Clear();
            if (types != null)
                foreach (var t in types)
                    AvailableTypes.Add(t);

            // Priorities from dedicated endpoint
            AvailablePriorities.Clear();
            using (var prioDoc = JsonDocument.Parse(prioritiesTask.Result))
            {
                Priority[]? priorities = null;
                if (prioDoc.RootElement.TryGetProperty("priorities", out var prioEl))
                    priorities = JsonSerializer.Deserialize<Priority[]>(prioEl.GetRawText(), options);
                else
                    priorities = JsonSerializer.Deserialize<Priority[]>(prioritiesTask.Result, options);

                if (priorities != null)
                    foreach (var p in priorities)
                        AvailablePriorities.Add(p);
            }

            // Groups (wrapped: {"success":true,"groups":[...]})
            AvailableGroups.Clear();
            using (var groupDoc = JsonDocument.Parse(groupsTask.Result))
            {
                Group[]? groups = null;
                if (groupDoc.RootElement.TryGetProperty("groups", out var groupsEl))
                    groups = JsonSerializer.Deserialize<Group[]>(groupsEl.GetRawText(), options);
                else
                    groups = JsonSerializer.Deserialize<Group[]>(groupsTask.Result, options);

                if (groups != null)
                    foreach (var g in groups)
                        AvailableGroups.Add(g);
            }
        }
        catch { /* Filter options are best-effort */ }
    }

    private void UpdateActiveFilterCount()
    {
        var count = 0;
        if (SelectedPriorityFilter != null) count++;
        if (SelectedTypeFilter != null) count++;
        if (SelectedGroupFilter != null) count++;
        if (ShowOnlyMyTickets) count++;
        if (ShowOnlyFavorites) count++;
        if (ShowOnlyOverdue) count++;
        if (DateFrom.HasValue) count++;
        if (DateTo.HasValue) count++;
        if (ActiveFilter != "all") count++;
        ActiveFilterCount = count;
    }

    private IEnumerable<Ticket> ApplyLocalFilters(IEnumerable<Ticket> source)
    {
        var filtered = source;

        // Apply status filter (check both German translated and English original names)
        filtered = ActiveFilter switch
        {
            "open" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() is "offen" or "open" or "neu" or "new"),
            "pending" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() is "ausstehend" or "pending" or "in bearbeitung" or "in progress" or "wartend" or "on hold"),
            "closed" => filtered.Where(t => t.Status?.IsResolved == true ||
                                            t.Status?.Name?.ToLowerInvariant() is "geschlossen" or "closed" or "gelöst" or "resolved"),
            _ => filtered
        };

        // Apply advanced filters
        if (SelectedPriorityFilter != null)
            filtered = filtered.Where(t => t.Priority?.Id == SelectedPriorityFilter.Id);

        if (SelectedTypeFilter != null)
            filtered = filtered.Where(t => t.Type?.Id == SelectedTypeFilter.Id);

        if (SelectedGroupFilter != null)
            filtered = filtered.Where(t => t.Group?.Id == SelectedGroupFilter.Id);

        if (ShowOnlyMyTickets && !string.IsNullOrEmpty(_apiService.CurrentUserId))
            filtered = filtered.Where(t => t.Assignee?.Id == _apiService.CurrentUserId);

        if (ShowOnlyFavorites)
            filtered = filtered.Where(t => _favoriteIds.Contains(t.Id));

        if (ShowOnlyOverdue)
            filtered = filtered.Where(t => t.DueDate != DateTime.MinValue && t.DueDate < DateTime.Now && t.Status?.IsResolved != true);

        if (DateFrom.HasValue)
            filtered = filtered.Where(t => t.Date >= DateFrom.Value);

        if (DateTo.HasValue)
            filtered = filtered.Where(t => t.Date <= DateTo.Value.AddDays(1));

        return filtered;
    }

    private IOrderedEnumerable<Ticket> ApplySorting(IEnumerable<Ticket> source)
    {
        return ActiveSort switch
        {
            "date_asc" => source.OrderBy(t => t.Date),
            "date_desc" => source.OrderByDescending(t => t.Date),
            "updated" => source.OrderByDescending(t => t.Updated),
            "priority" => source.OrderByDescending(t => t.Priority?.OverdueIn ?? 0),
            "subject" => source.OrderBy(t => t.Subject, StringComparer.OrdinalIgnoreCase),
            "duedate" => source.OrderBy(t => t.DueDate == DateTime.MinValue ? DateTime.MaxValue : t.DueDate),
            _ => source.OrderByDescending(t => t.Date)
        };
    }

    private void ApplyFilters()
    {
        var filtered = _allTickets.AsEnumerable();

        // Apply client-side search filter (only when not using server search)
        if (!_isServerSearchActive && !string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Subject?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Issue?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Owner?.Fullname?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Assignee?.Fullname?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.Uid.ToString().Contains(search)) ||
                (t.Tags?.Any(tag => tag.Name?.ToLowerInvariant().Contains(search) ?? false) ?? false)
            );
        }

        filtered = ApplyLocalFilters(filtered);
        var sorted = ApplySorting(filtered);

        FilteredTickets.Clear();
        foreach (var ticket in sorted)
        {
            FilteredTickets.Add(ticket);
        }

        UpdateActiveFilterCount();
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

        // Load favorites
        _favoriteIds = await _databaseService.GetFavoriteIdsAsync();
        await LoadSavedFiltersAsync();

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
    private async Task ShowScannerAsync()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is NavigationPage nav)
        {
            var scannerPage = _serviceProvider.GetRequiredService<Views.ScannerPage>();
            await nav.PushAsync(scannerPage);
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
                await _syncService.EnqueueAssignAsync(ticket.Id, ticket.Uid, userId, ticket.Updated);
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
                var statuses = Utils.JsonHelper.DeserializeWrappedArray<Status>(statusJson, "status", options);
                var closedStatus = statuses.FirstOrDefault(s => s.IsResolved);
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

    [RelayCommand]
    private void ToggleBulkSelectMode()
    {
        IsBulkSelectMode = !IsBulkSelectMode;
    }

    [RelayCommand]
    private void ToggleTicketSelection(Ticket ticket)
    {
        if (ticket == null) return;

        if (SelectedTicketIds.Contains(ticket.Id))
            SelectedTicketIds.Remove(ticket.Id);
        else
            SelectedTicketIds.Add(ticket.Id);

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedTickets));
    }

    [RelayCommand]
    private void SelectAllTickets()
    {
        SelectedTicketIds.Clear();
        foreach (var ticket in FilteredTickets)
            SelectedTicketIds.Add(ticket.Id);
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedTickets));
    }

    public bool IsTicketSelected(string ticketId) => SelectedTicketIds.Contains(ticketId);

    [RelayCommand]
    private async Task BulkAssignToMeAsync()
    {
        if (SelectedTicketIds.Count == 0) return;
        var userId = _apiService.CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            StatusMessage = "Benutzer-ID nicht verfügbar.";
            OnPropertyChanged(nameof(HasStatusMessage));
            return;
        }

        IsLoading = true;
        var successCount = 0;
        foreach (var ticketId in SelectedTicketIds.ToList())
        {
            try
            {
                if (await _apiService.AssignTicketAsync(ticketId, userId))
                    successCount++;
            }
            catch
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    var t = _allTickets.FirstOrDefault(x => x.Id == ticketId);
                    await _syncService.EnqueueAssignAsync(ticketId, t?.Uid ?? 0, userId, t?.Updated);
                }
            }
        }

        StatusMessage = $"{successCount} Ticket(s) zugewiesen.";
        IsBulkSelectMode = false;
        IsLoading = false;
        OnPropertyChanged(nameof(HasStatusMessage));
        await LoadTicketsAsync();
    }

    [RelayCommand]
    private async Task BulkCloseAsync()
    {
        if (SelectedTicketIds.Count == 0) return;

        IsLoading = true;
        Status? closedStatus = null;
        try
        {
            var statusJson = await _apiService.GetStatusesAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var statuses = Utils.JsonHelper.DeserializeWrappedArray<Status>(statusJson, "status", options);
            closedStatus = statuses.FirstOrDefault(s => s.IsResolved);
        }
        catch { }

        var successCount = 0;
        foreach (var ticketId in SelectedTicketIds.ToList())
        {
            try
            {
                var ticket = _allTickets.FirstOrDefault(t => t.Id == ticketId);
                if (ticket == null) continue;

                var editTicket = new Ticket
                {
                    Id = ticketId,
                    Subject = ticket.Subject,
                    Issue = ticket.Issue,
                    Priority = ticket.Priority,
                    Status = closedStatus ?? new Status { Id = ticket.Status?.Id, Name = "Closed", IsResolved = true }
                };

                if (await _apiService.EditTicketAsync(editTicket))
                    successCount++;
            }
            catch { }
        }

        StatusMessage = $"{successCount} Ticket(s) geschlossen.";
        IsBulkSelectMode = false;
        IsLoading = false;
        OnPropertyChanged(nameof(HasStatusMessage));
        await LoadTicketsAsync();
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

    [RelayCommand]
    private async Task ExportTicketsAsync()
    {
        if (FilteredTickets.Count == 0)
        {
            StatusMessage = "Keine Tickets zum Exportieren.";
            OnPropertyChanged(nameof(HasStatusMessage));
            return;
        }

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Nr;Betreff;Status;Priorität;Gruppe;Zugewiesen;Erstellt;Aktualisiert;Fällig");

            foreach (var t in FilteredTickets)
            {
                var uid = t.Uid.ToString();
                var subject = EscapeCsvField(t.Subject);
                var status = t.Status?.Name ?? "";
                var priority = t.Priority?.Name ?? "";
                var group = t.Group?.Name ?? "";
                var assignee = t.Assignee?.Fullname ?? "Nicht zugewiesen";
                var created = t.Date != DateTime.MinValue ? t.Date.ToString("dd.MM.yyyy HH:mm") : "";
                var updated = t.Updated != DateTime.MinValue ? t.Updated.ToString("dd.MM.yyyy HH:mm") : "";
                var dueDate = t.DueDate != DateTime.MinValue ? t.DueDate.ToString("dd.MM.yyyy HH:mm") : "";

                sb.AppendLine($"{uid};{subject};{status};{priority};{group};{assignee};{created};{updated};{dueDate}");
            }

            var fileName = $"Tickets_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, sb.ToString(), System.Text.Encoding.UTF8);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Tickets exportieren",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export fehlgeschlagen: {ex.Message}";
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }
}

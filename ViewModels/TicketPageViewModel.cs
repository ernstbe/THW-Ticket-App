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
        set => SetProperty(ref _searchText, value);
    }

    private string _activeFilter = "all";
    public string ActiveFilter
    {
        get => _activeFilter;
        set => SetProperty(ref _activeFilter, value);
    }

    private bool _isOfflineMode;
    public bool IsOfflineMode
    {
        get => _isOfflineMode;
        set => SetProperty(ref _isOfflineMode, value);
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public TicketPageViewModel(TrueDeskApiService apiService, IServiceProvider serviceProvider, DatabaseService databaseService)
    {
        _apiService = apiService;
        _serviceProvider = serviceProvider;
        _databaseService = databaseService;
        _isOfflineCacheEnabled = Preferences.Get("OfflineCache", false);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
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

        // Apply status filter
        filtered = ActiveFilter switch
        {
            "open" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() == "open" ||
                                          t.Status?.Name?.ToLowerInvariant() == "neu" ||
                                          t.Status?.Name?.ToLowerInvariant() == "new"),
            "pending" => filtered.Where(t => t.Status?.Name?.ToLowerInvariant() == "pending" ||
                                             t.Status?.Name?.ToLowerInvariant() == "in bearbeitung" ||
                                             t.Status?.Name?.ToLowerInvariant() == "in progress"),
            "closed" => filtered.Where(t => t.Status?.IsResolved == true ||
                                            t.Status?.Name?.ToLowerInvariant() == "closed" ||
                                            t.Status?.Name?.ToLowerInvariant() == "geschlossen" ||
                                            t.Status?.Name?.ToLowerInvariant() == "resolved"),
            _ => filtered
        };

        FilteredTickets.Clear();
        foreach (var ticket in filtered.OrderByDescending(t => t.Date))
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
        catch (HttpRequestException)
        {
            // Try to load from cache
            await LoadFromCacheAsync("Verbindungsfehler. Zeige gecachte Daten.");
        }
        catch (JsonException)
        {
            StatusMessage = "Fehler beim Verarbeiten der Daten.";
        }
        catch (Exception)
        {
            // Try to load from cache
            await LoadFromCacheAsync("Fehler beim Laden. Zeige gecachte Daten.");
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
    public async Task RefreshTickets()
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
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }
    }
}

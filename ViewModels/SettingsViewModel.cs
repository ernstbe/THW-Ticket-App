using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;
    private readonly AppSettings _appSettings;
    private readonly DatabaseService _databaseService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isNotificationsEnabled;

    [ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string _connectionTimeout = "30";

    [ObservableProperty]
    private bool _isOfflineCacheEnabled;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private Color _connectionStatusColor = Colors.Gray;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _cacheInfo = string.Empty;

    public bool HasConnectionStatus => !string.IsNullOrEmpty(ConnectionStatus);

    public SettingsViewModel(TrueDeskApiService apiService, AppSettings appSettings, DatabaseService databaseService, NotificationService notificationService)
    {
        _apiService = apiService;
        _appSettings = appSettings;
        _databaseService = databaseService;
        _notificationService = notificationService;
    }

    public async void LoadSettings()
    {
        // Load from preferences
        IsDarkMode = Preferences.Get("DarkMode", false);
        IsNotificationsEnabled = _notificationService.IsEnabled;
        ApiBaseUrl = Preferences.Get("ApiBaseUrl", _appSettings.ApiBaseUrl);
        ConnectionTimeout = Preferences.Get("ConnectionTimeout", _appSettings.ConnectionTimeoutSeconds).ToString();
        IsOfflineCacheEnabled = Preferences.Get("OfflineCache", false);

        // Load cache info
        await LoadCacheInfoAsync();

        // Apply theme
        ApplyTheme();
    }

    private async Task LoadCacheInfoAsync()
    {
        try
        {
            var count = await _databaseService.GetCachedTicketCountAsync();
            var lastCache = await _databaseService.GetLastCacheTimeAsync();

            if (count > 0 && lastCache.HasValue)
            {
                CacheInfo = $"{count} Tickets im Cache (Stand: {lastCache.Value:dd.MM.yyyy HH:mm})";
            }
            else
            {
                CacheInfo = "Kein Cache vorhanden";
            }
        }
        catch
        {
            CacheInfo = "Cache-Info nicht verfügbar";
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        ApplyTheme();
    }

    partial void OnIsNotificationsEnabledChanged(bool value)
    {
        _notificationService.IsEnabled = value;
    }

    private void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = IsDarkMode ? AppTheme.Dark : AppTheme.Light;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionStatus = "Teste Verbindung...";
        ConnectionStatusColor = Colors.Gray;
        OnPropertyChanged(nameof(HasConnectionStatus));

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.GetAsync($"{ApiBaseUrl.TrimEnd('/')}/");

            if (response.IsSuccessStatusCode)
            {
                ConnectionStatus = "Verbindung erfolgreich!";
                ConnectionStatusColor = Colors.Green;
            }
            else
            {
                ConnectionStatus = $"Fehler: {response.StatusCode}";
                ConnectionStatusColor = Colors.Red;
            }
        }
        catch (HttpRequestException)
        {
            ConnectionStatus = "Verbindung fehlgeschlagen. Server nicht erreichbar.";
            ConnectionStatusColor = Colors.Red;
        }
        catch (TaskCanceledException)
        {
            ConnectionStatus = "Zeitüberschreitung. Server antwortet nicht.";
            ConnectionStatusColor = Colors.Orange;
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Fehler: {ex.Message}";
            ConnectionStatusColor = Colors.Red;
        }

        OnPropertyChanged(nameof(HasConnectionStatus));
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            await _databaseService.ClearCacheAsync();
            await LoadCacheInfoAsync();

            await Application.Current!.MainPage!.DisplayAlert(
                "Cache geleert",
                "Der Cache wurde erfolgreich geleert.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Fehler",
                $"Cache konnte nicht geleert werden: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        // Validate timeout
        if (!int.TryParse(ConnectionTimeout, out var timeout) || timeout < 5 || timeout > 300)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Ungültige Eingabe",
                "Timeout muss zwischen 5 und 300 Sekunden liegen.",
                "OK");
            return;
        }

        // Save to preferences
        Preferences.Set("DarkMode", IsDarkMode);
        Preferences.Set("ApiBaseUrl", ApiBaseUrl);
        Preferences.Set("ConnectionTimeout", timeout);
        Preferences.Set("OfflineCache", IsOfflineCacheEnabled);
        _notificationService.IsEnabled = IsNotificationsEnabled;

        // Update app settings
        _appSettings.ApiBaseUrl = ApiBaseUrl;
        _appSettings.ConnectionTimeoutSeconds = timeout;

        await Application.Current!.MainPage!.DisplayAlert(
            "Gespeichert",
            "Die Einstellungen wurden gespeichert. Einige Änderungen werden erst nach einem Neustart wirksam.",
            "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Abmelden",
            "Möchten Sie sich wirklich abmelden?",
            "Ja",
            "Nein");

        if (confirm)
        {
            _apiService.Logout();

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                await nav.Navigation.PopToRootAsync();
            }
        }
    }
}

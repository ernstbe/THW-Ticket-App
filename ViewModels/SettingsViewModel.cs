using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Fingerprint.Abstractions;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;
    private readonly AppSettings _appSettings;
    private readonly DatabaseService _databaseService;
    private readonly NotificationService _notificationService;
    private readonly IFingerprint _fingerprint;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isNotificationsEnabled;

    [ObservableProperty]
    private bool _notifyOnlyMyTickets;

    [ObservableProperty]
    private bool _notifyOnlyHighPriority;

    [ObservableProperty]
    private bool _notifyOnNewTickets;

    [ObservableProperty]
    private bool _notifyOnComments;

    [ObservableProperty]
    private bool _notifyOnStatusChanges;

    [ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string _connectionTimeout = "30";

    [ObservableProperty]
    private bool _isOfflineCacheEnabled;

    [ObservableProperty]
    private bool _isBiometricEnabled;

    [ObservableProperty]
    private bool _isBiometricSupported;

    [ObservableProperty]
    private int _selectedLanguageIndex;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private Color _connectionStatusColor = Colors.Gray;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _cacheInfo = string.Empty;

    public bool HasConnectionStatus => !string.IsNullOrEmpty(ConnectionStatus);

    public SettingsViewModel(TrueDeskApiService apiService, AppSettings appSettings, DatabaseService databaseService, NotificationService notificationService, IFingerprint fingerprint)
    {
        _apiService = apiService;
        _appSettings = appSettings;
        _databaseService = databaseService;
        _notificationService = notificationService;
        _fingerprint = fingerprint;
    }

    public async void LoadSettings()
    {
        // Load from preferences
        IsDarkMode = Preferences.Get("DarkMode", false);
        IsNotificationsEnabled = _notificationService.IsEnabled;
        NotifyOnlyMyTickets = _notificationService.NotifyOnlyMyTickets;
        NotifyOnlyHighPriority = _notificationService.NotifyOnlyHighPriority;
        NotifyOnNewTickets = _notificationService.NotifyOnNewTickets;
        NotifyOnComments = _notificationService.NotifyOnComments;
        NotifyOnStatusChanges = _notificationService.NotifyOnStatusChanges;
        ApiBaseUrl = Preferences.Get("ApiBaseUrl", _appSettings.ApiBaseUrl);
        ConnectionTimeout = Preferences.Get("ConnectionTimeout", _appSettings.ConnectionTimeoutSeconds).ToString();
        IsOfflineCacheEnabled = Preferences.Get("OfflineCache", false);
        IsBiometricEnabled = Preferences.Get("BiometricEnabled", false);
        var currentLang = Preferences.Get("AppLanguage", "de");
        SelectedLanguageIndex = Array.IndexOf(LocalizationService.SupportedLanguages, currentLang);
        if (SelectedLanguageIndex < 0) SelectedLanguageIndex = 0;

        try { IsBiometricSupported = await _fingerprint.IsAvailableAsync(); } catch { }

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

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlertAsync(
                    "Cache geleert",
                    "Der Cache wurde erfolgreich geleert.",
                    "OK");
        }
        catch (Exception ex)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlertAsync(
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
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlertAsync(
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
        Preferences.Set("BiometricEnabled", IsBiometricEnabled);
        if (SelectedLanguageIndex >= 0 && SelectedLanguageIndex < LocalizationService.SupportedLanguages.Length)
        {
            LocalizationService.Instance.SetLanguage(LocalizationService.SupportedLanguages[SelectedLanguageIndex]);
        }
        _notificationService.IsEnabled = IsNotificationsEnabled;
        _notificationService.NotifyOnlyMyTickets = NotifyOnlyMyTickets;
        _notificationService.NotifyOnlyHighPriority = NotifyOnlyHighPriority;
        _notificationService.NotifyOnNewTickets = NotifyOnNewTickets;
        _notificationService.NotifyOnComments = NotifyOnComments;
        _notificationService.NotifyOnStatusChanges = NotifyOnStatusChanges;

        // Update app settings
        _appSettings.ApiBaseUrl = ApiBaseUrl;
        _appSettings.ConnectionTimeoutSeconds = timeout;

        var savePage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (savePage != null)
            await savePage.DisplayAlertAsync(
                "Gespeichert",
                "Die Einstellungen wurden gespeichert. Einige Änderungen werden erst nach einem Neustart wirksam.",
                "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return;

        var confirm = await page.DisplayAlertAsync(
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

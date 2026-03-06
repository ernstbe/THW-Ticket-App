using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace THWTicketApp.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private static readonly ResourceManager _resourceManager =
        new("THWTicketApp.Resources.Strings.AppResources", typeof(LocalizationService).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly string[] SupportedLanguages = ["de", "en"];
    public static readonly string[] LanguageDisplayNames = ["Deutsch", "English"];

    public string CurrentLanguage { get; private set; } = "de";

    public LocalizationService()
    {
        var savedLang = Preferences.Get("AppLanguage", "de");
        SetLanguage(savedLang, save: false);
    }

    public void SetLanguage(string languageCode, bool save = true)
    {
        if (!SupportedLanguages.Contains(languageCode))
            languageCode = "de";

        CurrentLanguage = languageCode;
        var culture = new CultureInfo(languageCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (save)
            Preferences.Set("AppLanguage", languageCode);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public string this[string key] => GetString(key);

    public static string GetString(string key)
    {
        try
        {
            return _resourceManager.GetString(key, CultureInfo.DefaultThreadCurrentUICulture) ?? key;
        }
        catch
        {
            return key;
        }
    }
}

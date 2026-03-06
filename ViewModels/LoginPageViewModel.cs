using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly TrueDeskApiService _apiService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _loginStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public LoginPageViewModel(TrueDeskApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                LoginStatus = "Bitte Benutzername und Passwort eingeben.";
                return;
            }

            IsLoading = true;
            LoginStatus = "Anmelden...";

            try
            {
                var success = await _apiService.AuthenticateAsync(Username, Password);

                if (success)
                {
                    LoginStatus = "Anmeldung erfolgreich!";
                    Password = string.Empty; // Clear password from memory

                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window?.Page is NavigationPage nav)
                    {
                        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
                        await nav.PushAsync(mainPage);
                    }
                }
                else
                {
                    LoginStatus = "Anmeldung fehlgeschlagen. Ungültige Anmeldedaten.";
                }
            }
            catch (Exception)
            {
                LoginStatus = "Netzwerkfehler. Bitte Verbindung prüfen.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task TryAutoLoginAsync()
        {
            if (await _apiService.TryRestoreSessionAsync())
            {
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window?.Page is NavigationPage nav)
                {
                    var mainPage = _serviceProvider.GetRequiredService<MainPage>();
                    await nav.PushAsync(mainPage);
                }
            }
        }
    }
}

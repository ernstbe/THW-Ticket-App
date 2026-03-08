using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Fingerprint.Abstractions;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly ITrueDeskApiService _apiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFingerprint _fingerprint;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _loginStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isBiometricAvailable;

        public LoginPageViewModel(ITrueDeskApiService apiService, IServiceProvider serviceProvider, IFingerprint fingerprint)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
            _fingerprint = fingerprint;
            _ = CheckBiometricAsync();
        }

        private async Task CheckBiometricAsync()
        {
            try
            {
                var available = await _fingerprint.IsAvailableAsync();
                var hasSavedCredentials = Preferences.Get("BiometricEnabled", false);
                IsBiometricAvailable = available && hasSavedCredentials;
            }
            catch
            {
                IsBiometricAvailable = false;
            }
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

                    // Save credentials for biometric login
                    try
                    {
                        await SecureStorage.SetAsync("bio_username", Username);
                        await SecureStorage.SetAsync("bio_password", Password);
                        Preferences.Set("BiometricEnabled", true);
                    }
                    catch { }

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
        private async Task BiometricLoginAsync()
        {
            try
            {
                var request = new AuthenticationRequestConfiguration(
                    "THW Ticket App",
                    "Bitte authentifizieren Sie sich, um sich anzumelden.")
                {
                    AllowAlternativeAuthentication = true
                };

                var result = await _fingerprint.AuthenticateAsync(request);
                if (result.Authenticated)
                {
                    IsLoading = true;
                    LoginStatus = "Anmelden...";

                    var username = await SecureStorage.GetAsync("bio_username");
                    var password = await SecureStorage.GetAsync("bio_password");

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        var success = await _apiService.AuthenticateAsync(username, password);
                        if (success)
                        {
                            LoginStatus = "Anmeldung erfolgreich!";
                            var window = Application.Current?.Windows.FirstOrDefault();
                            if (window?.Page is NavigationPage nav)
                            {
                                var mainPage = _serviceProvider.GetRequiredService<MainPage>();
                                await nav.PushAsync(mainPage);
                            }
                        }
                        else
                        {
                            LoginStatus = "Gespeicherte Anmeldedaten ungültig. Bitte manuell anmelden.";
                            Preferences.Set("BiometricEnabled", false);
                            IsBiometricAvailable = false;
                        }
                    }
                }
                else
                {
                    LoginStatus = "Authentifizierung abgebrochen.";
                }
            }
            catch (Exception)
            {
                LoginStatus = "Biometrische Authentifizierung fehlgeschlagen.";
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

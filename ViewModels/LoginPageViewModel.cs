using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly TicketPage _ticketPage;
        private readonly TrueDeskApiService _apiService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _loginStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public LoginPageViewModel(TrueDeskApiService apiService, TicketPage ticketPage)
        {
            _apiService = apiService;
            _ticketPage = ticketPage;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                LoginStatus = "Please enter username and password.";
                return;
            }

            IsLoading = true;
            LoginStatus = "Logging in...";

            try
            {
                var success = await _apiService.AuthenticateAsync(Username, Password);

                if (success)
                {
                    LoginStatus = "Login successful!";
                    Password = string.Empty; // Clear password from memory

                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window?.Page is NavigationPage nav)
                    {
                        await nav.PushAsync(_ticketPage);
                    }
                }
                else
                {
                    LoginStatus = "Login failed. Please check your credentials.";
                }
            }
            catch (Exception)
            {
                LoginStatus = "Connection error. Please check your network.";
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
                    await nav.PushAsync(_ticketPage);
                }
            }
        }
    }
}

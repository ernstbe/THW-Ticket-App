using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class LoginPageViewModel(TrueDeskApiService apiService, TicketPage ticketPage) : ObservableObject
    {
        private readonly TicketPage ticketPage = ticketPage;
        private readonly TrueDeskApiService _apiService = apiService;
        [ObservableProperty]
        private string _username = string.Empty;
        [ObservableProperty]
        private string _password = string.Empty;
        [ObservableProperty]
        private string _loginStatus = string.Empty;

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
            {
                Username = "Ernstbe";
                Password = "Darkben123";
            }
            LoginStatus = "Logging in...";
            var success = await _apiService.AuthenticateAsync(Username, Password);
            if (success)
            {
                LoginStatus = "Login successful!";
                // Use PushAsync for navigation
                if (Application.Current?.MainPage is NavigationPage nav)
                {
                    await nav.PushAsync(ticketPage);
                }
            }
            else
            {
                LoginStatus = "Login failed. Check credentials.";
            }
        }
    }
}

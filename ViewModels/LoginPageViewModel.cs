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
                        var ticketPage = _serviceProvider.GetRequiredService<TicketPage>();
                        await nav.PushAsync(ticketPage);
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
                    var ticketPage = _serviceProvider.GetRequiredService<TicketPage>();
                    await nav.PushAsync(ticketPage);
                }
            }
        }
    }
}

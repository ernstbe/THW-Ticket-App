using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class LoginPageViewModel : ObservableObject
    {
    private readonly TrueDeskApiService _apiService;
    [ObservableProperty]
    private string _username = string.Empty;
    [ObservableProperty]
    private string _password = string.Empty;
    [ObservableProperty]
    private string _loginStatus = string.Empty;

        public LoginPageViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
        }

        [RelayCommand]
        private async Task Login()
        {
            LoginStatus = "Logging in...";
            var success = await _apiService.AuthenticateAsync(Username, Password);
            if (success)
            {
                LoginStatus = "Login successful!";
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new TicketPage(_apiService);
                }
            }
            else
            {
                LoginStatus = "Login failed. Check credentials.";
            }
        }
    }
}

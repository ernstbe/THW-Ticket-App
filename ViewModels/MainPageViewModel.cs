using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly TrueDeskApiService _apiService;

        [ObservableProperty]
        private string _welcomeMessage = "Welcome to THW Ticket App";

        [ObservableProperty]
        private bool _isAuthenticated;

        public MainPageViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
            IsAuthenticated = _apiService.IsAuthenticated;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            _apiService.Logout();
            IsAuthenticated = false;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                await nav.Navigation.PopToRootAsync();
            }
        }
    }
}

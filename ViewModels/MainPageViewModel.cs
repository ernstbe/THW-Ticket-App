<<<<<<< HEAD
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace THWTicketApp.ViewModels
{
    public class MainPageViewModel : BindableObject
    {
        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public ICommand CounterCommand { get; }

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public MainPageViewModel()
        {
            CounterCommand = new Command(OnCounterClicked);
        }

        private void OnCounterClicked()
        {
            Count++;
            OnPropertyChanged(nameof(CounterText));
=======
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
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }
    }
}

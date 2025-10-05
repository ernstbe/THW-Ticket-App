using THWTicketApp.Services;

namespace THWTicketApp;

public partial class LoginPage : ContentPage
{
    private readonly TrueDeskApiService _apiService = new TrueDeskApiService();
    private readonly ViewModels.LoginPageViewModel _viewModel;

    public LoginPage()
    {
        InitializeComponent();
        _viewModel = new ViewModels.LoginPageViewModel(_apiService);
        BindingContext = _viewModel;
    }
}

using THWTicketApp.ViewModels;

namespace THWTicketApp;

public partial class LoginPage : ContentPage
{
    private readonly ViewModels.LoginPageViewModel _viewModel;

    public LoginPage(LoginPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}

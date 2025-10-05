namespace THWTicketApp;

public partial class MainPage : ContentPage
{

    private readonly ViewModels.MainPageViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new ViewModels.MainPageViewModel();
        BindingContext = _viewModel;
    }
}

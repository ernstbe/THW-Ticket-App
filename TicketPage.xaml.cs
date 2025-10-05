namespace THWTicketApp;

public partial class TicketPage : ContentPage
{

    private readonly ViewModels.TicketPageViewModel _viewModel;

    public TicketPage(ViewModels.TicketPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTickets();
    }
}

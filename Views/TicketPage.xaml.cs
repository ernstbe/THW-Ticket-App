using THWTicketApp.Services;

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{

    private readonly ViewModels.TicketPageViewModel _viewModel;
    private readonly TrueDeskApiService _apiService;


    public TicketPage(ViewModels.TicketPageViewModel viewModel, TrueDeskApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        TicketsCollectionView.SelectionChanged += TicketsCollectionView_SelectionChanged;
    }

    private async void TicketsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Ticket selectedTicket)
        {
            var apiService = _viewModel.ApiService;
            await Navigation.PushAsync(new Views.TicketDetailPage(selectedTicket, apiService));
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTickets();
    }
}

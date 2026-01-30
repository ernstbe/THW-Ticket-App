using THWTicketApp.Services;
<<<<<<< HEAD
=======
using THWTicketApp.Views;
using THWTicketApp.ViewModels;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{
<<<<<<< HEAD

    private readonly ViewModels.TicketPageViewModel _viewModel;
    private readonly TrueDeskApiService _apiService;


    public TicketPage(ViewModels.TicketPageViewModel viewModel, TrueDeskApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
=======
    private readonly TicketPageViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public TicketPage(TicketPageViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        BindingContext = _viewModel;
        TicketsCollectionView.SelectionChanged += TicketsCollectionView_SelectionChanged;
    }

<<<<<<< HEAD
    private async void TicketsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Ticket selectedTicket)
        {
            var apiService = _viewModel.ApiService;
            await Navigation.PushAsync(new Views.TicketDetailPage(selectedTicket, apiService));
=======
    private async void TicketsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Ticket selectedTicket)
        {
            // Use DI to get a new TicketDetailPage instance
            var detailPage = _serviceProvider.GetRequiredService<TicketDetailPage>();
            detailPage.SetTicket(selectedTicket);
            await Navigation.PushAsync(detailPage);

            // Clear selection to allow re-selecting the same item
            TicketsCollectionView.SelectedItem = null;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
<<<<<<< HEAD
        await _viewModel.LoadTickets();
=======
        await _viewModel.LoadTicketsAsync();
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    }
}

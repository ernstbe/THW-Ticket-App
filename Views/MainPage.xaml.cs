using THWTicketApp.Models;
using THWTicketApp.ViewModels;

namespace THWTicketApp;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadDashboardCommand.ExecuteAsync(null);
    }

    private async void OnRecentTicketSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Ticket ticket)
        {
            await _viewModel.NavigateToTicketDetailCommand.ExecuteAsync(ticket);

            if (sender is CollectionView cv)
                cv.SelectedItem = null;
        }
    }
}

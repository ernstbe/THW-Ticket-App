using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class TeamDashboardPage : ContentPage
{
    private readonly TeamDashboardViewModel _viewModel;

    public TeamDashboardPage(TeamDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTeamDataAsync();
    }
}

using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class NotificationHistoryPage : ContentPage
{
    private readonly NotificationHistoryViewModel _viewModel;

    public NotificationHistoryPage(NotificationHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadNotificationsAsync();
    }
}

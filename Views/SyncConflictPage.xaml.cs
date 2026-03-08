using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class SyncConflictPage : ContentPage
{
    private readonly SyncConflictViewModel _viewModel;

    public SyncConflictPage(SyncConflictViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadConflictsAsync();
    }
}

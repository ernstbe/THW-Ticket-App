using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class ReportingPage : ContentPage
{
    private readonly ReportingViewModel _viewModel;

    public ReportingPage(ReportingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadReportAsync();
    }
}

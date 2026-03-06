
using THWTicketApp.Services;
using THWTicketApp.Views;
using THWTicketApp.ViewModels;

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{
    private readonly TicketPageViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    private static readonly string[] SortValues = ["date_desc", "date_asc", "updated", "priority", "duedate", "subject"];

    public TicketPage(TicketPageViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        TicketsCollectionView.SelectionChanged += TicketsCollectionView_SelectionChanged;
        SortPicker.SelectedIndex = 0;
    }

    private void OnSortChanged(object? sender, EventArgs e)
    {
        if (SortPicker.SelectedIndex >= 0 && SortPicker.SelectedIndex < SortValues.Length)
        {
            _viewModel.SortCommand.Execute(SortValues[SortPicker.SelectedIndex]);
        }
    }

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
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTicketsAsync();
    }
}

using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class KanbanBoardPage : ContentPage
{
    private readonly KanbanBoardViewModel _viewModel;

    public KanbanBoardPage(KanbanBoardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadBoardAsync();
    }
}

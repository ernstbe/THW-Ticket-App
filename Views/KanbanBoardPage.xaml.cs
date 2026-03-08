using THWTicketApp.Models;
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

    private void OnDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is GestureRecognizer gr && gr.Parent is View view && view.BindingContext is Ticket ticket)
        {
            e.Data.Properties["Ticket"] = ticket;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void OnDrop(object? sender, DropEventArgs e)
    {
        if (e.Data.Properties.TryGetValue("Ticket", out var ticketObj) && ticketObj is Ticket ticket)
        {
            string? targetStatusId = null;
            if (sender is GestureRecognizer gr && gr.Parent is View view && view.BindingContext is KanbanColumn column)
            {
                targetStatusId = column.StatusId;
            }

            if (!string.IsNullOrEmpty(targetStatusId))
            {
                await _viewModel.MoveTicketCommand.ExecuteAsync(new TicketMoveInfo
                {
                    Ticket = ticket,
                    TargetStatusId = targetStatusId
                });
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public class TicketMoveInfo
{
    public Ticket? Ticket { get; set; }
    public string? TargetStatusId { get; set; }
}

public partial class KanbanColumn : ObservableObject
{
    public string StatusId { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#9E9E9E";
    public ObservableCollection<Ticket> Tickets { get; set; } = new();
    public int TicketCount => Tickets.Count;
}

public partial class KanbanBoardViewModel : ObservableObject
{
    private readonly ITrueDeskApiService _apiService;
    private readonly ISyncService _syncService;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ObservableCollection<KanbanColumn> Columns { get; } = new();

    public KanbanBoardViewModel(ITrueDeskApiService apiService, ISyncService syncService)
    {
        _apiService = apiService;
        _syncService = syncService;
    }

    [RelayCommand]
    public async Task LoadBoardAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Load statuses and tickets in parallel
            var statusTask = _apiService.GetStatusesAsync();
            var ticketTask = _apiService.GetTicketsAsync();
            await Task.WhenAll(statusTask, ticketTask);

            var statuses = Utils.JsonHelper.DeserializeWrappedArray<Status>(statusTask.Result, "status", options);
            var tickets = Utils.JsonHelper.DeserializeWrappedArray<Ticket>(ticketTask.Result, "tickets", options);

            // Translate
            foreach (var t in tickets)
                TrudeskTranslationHelper.TranslateTicket(t);

            Columns.Clear();

            foreach (var status in statuses.OrderBy(s => s.Order))
            {
                var column = new KanbanColumn
                {
                    StatusId = status.Id ?? string.Empty,
                    StatusName = TrudeskTranslationHelper.TranslateStatus(status.Name) ?? status.Name ?? "Unknown",
                    StatusColor = status.HtmlColor ?? "#9E9E9E"
                };

                var columnTickets = tickets.Where(t => t.Status?.Id == status.Id)
                    .OrderByDescending(t => t.Priority?.OverdueIn ?? 0);

                foreach (var ticket in columnTickets)
                    column.Tickets.Add(ticket);

                Columns.Add(column);
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshBoardAsync()
    {
        IsRefreshing = true;
        await LoadBoardAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task MoveTicketAsync(TicketMoveInfo moveInfo)
    {
        if (moveInfo.Ticket == null || string.IsNullOrEmpty(moveInfo.TargetStatusId)) return;

        // Don't move if same column
        if (moveInfo.Ticket.Status?.Id == moveInfo.TargetStatusId) return;

        try
        {
            var success = await _apiService.UpdateTicketStatusAsync(moveInfo.Ticket.Id!, moveInfo.TargetStatusId);
            if (success)
            {
                await LoadBoardAsync();
            }
        }
        catch
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await _syncService.EnqueueUpdateStatusAsync(
                    moveInfo.Ticket.Id!, moveInfo.Ticket.Uid, moveInfo.TargetStatusId, moveInfo.Ticket.Updated);
            }
        }
    }
}

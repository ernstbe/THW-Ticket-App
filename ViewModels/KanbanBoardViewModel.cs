using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class KanbanColumn : ObservableObject
{
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#9E9E9E";
    public ObservableCollection<Ticket> Tickets { get; set; } = new();
    public int TicketCount => Tickets.Count;
}

public partial class KanbanBoardViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<KanbanColumn> Columns { get; } = new();

    public KanbanBoardViewModel(TrueDeskApiService apiService)
    {
        _apiService = apiService;
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

            var statuses = JsonSerializer.Deserialize<Status[]>(statusTask.Result, options) ?? [];
            var tickets = JsonSerializer.Deserialize<Ticket[]>(ticketTask.Result, options) ?? [];

            // Translate
            foreach (var t in tickets)
                TrudeskTranslationHelper.TranslateTicket(t);

            Columns.Clear();

            foreach (var status in statuses.OrderBy(s => s.Order))
            {
                var column = new KanbanColumn
                {
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
}

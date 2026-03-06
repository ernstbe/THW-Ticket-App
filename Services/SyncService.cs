using System.Text.Json;

namespace THWTicketApp.Services;

public class SyncService
{
    private readonly DatabaseService _databaseService;
    private readonly TrueDeskApiService _apiService;
    private bool _isSyncing;

    public event Action<int>? PendingCountChanged;

    public SyncService(DatabaseService databaseService, TrueDeskApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;

        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            await SyncPendingActionsAsync();
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _databaseService.GetPendingActionCountAsync();
    }

    public async Task EnqueueCommentAsync(string ticketId, string ownerId, string comment)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, ownerId, comment });
        await _databaseService.EnqueueActionAsync("AddComment", payload);
        await NotifyCountChanged();
    }

    public async Task EnqueueNoteAsync(string ticketId, string ownerId, string note)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, ownerId, note });
        await _databaseService.EnqueueActionAsync("AddNote", payload);
        await NotifyCountChanged();
    }

    public async Task EnqueueCreateTicketAsync(string subject, string? issue, string? typeId, string? priorityId, string? groupId, string? assigneeId)
    {
        var payload = JsonSerializer.Serialize(new { subject, issue, typeId, priorityId, groupId, assigneeId });
        await _databaseService.EnqueueActionAsync("CreateTicket", payload);
        await NotifyCountChanged();
    }

    public async Task EnqueueAssignAsync(string ticketId, string userId)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, userId });
        await _databaseService.EnqueueActionAsync("AssignTicket", payload);
        await NotifyCountChanged();
    }

    public async Task<bool> SyncPendingActionsAsync()
    {
        if (_isSyncing || !_apiService.IsAuthenticated) return false;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        _isSyncing = true;
        var allSucceeded = true;

        try
        {
            var actions = await _databaseService.GetPendingActionsAsync();
            foreach (var action in actions)
            {
                var success = await ProcessActionAsync(action);
                if (success)
                {
                    await _databaseService.RemoveActionAsync(action.Id);
                }
                else
                {
                    await _databaseService.IncrementRetryCountAsync(action.Id);
                    if (action.RetryCount >= 5)
                    {
                        // Give up after 5 retries
                        await _databaseService.RemoveActionAsync(action.Id);
                    }
                    allSucceeded = false;
                }
            }

            await NotifyCountChanged();
        }
        finally
        {
            _isSyncing = false;
        }

        return allSucceeded;
    }

    private async Task<bool> ProcessActionAsync(Data.PendingAction action)
    {
        try
        {
            var doc = JsonDocument.Parse(action.PayloadJson);
            var root = doc.RootElement;

            return action.ActionType switch
            {
                "AddComment" => await _apiService.AddCommentAsync(
                    root.GetProperty("ticketId").GetString()!,
                    root.GetProperty("ownerId").GetString()!,
                    root.GetProperty("comment").GetString()!),

                "AddNote" => await _apiService.AddNoteAsync(
                    root.GetProperty("ticketId").GetString()!,
                    root.GetProperty("ownerId").GetString()!,
                    root.GetProperty("note").GetString()!),

                "CreateTicket" => await _apiService.CreateTicketAsync(
                    root.GetProperty("subject").GetString()!,
                    root.GetProperty("issue").GetString(),
                    root.GetProperty("typeId").GetString(),
                    root.GetProperty("priorityId").GetString(),
                    root.GetProperty("groupId").GetString(),
                    root.GetProperty("assigneeId").GetString()),

                "AssignTicket" => await _apiService.AssignTicketAsync(
                    root.GetProperty("ticketId").GetString()!,
                    root.GetProperty("userId").GetString()!),

                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private async Task NotifyCountChanged()
    {
        var count = await _databaseService.GetPendingActionCountAsync();
        PendingCountChanged?.Invoke(count);
    }
}

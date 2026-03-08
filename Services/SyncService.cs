using System.Text.Json;
using THWTicketApp.Models;

namespace THWTicketApp.Services;

public class SyncService : ISyncService
{
    private readonly IDatabaseService _databaseService;
    private readonly ITrueDeskApiService _apiService;
    private bool _isSyncing;

    public event Action<int>? PendingCountChanged;
    public event Action<Data.PendingAction>? ConflictDetected;

    public SyncService(IDatabaseService databaseService, ITrueDeskApiService apiService)
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

    public async Task EnqueueCommentAsync(string ticketId, string ownerId, string comment, DateTime? ticketUpdatedAt = null)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, ownerId, comment });
        await _databaseService.EnqueueActionAsync("AddComment", payload, ticketUpdatedAt);
        await NotifyCountChanged();
    }

    public async Task EnqueueNoteAsync(string ticketId, string ownerId, string note, DateTime? ticketUpdatedAt = null)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, ownerId, note });
        await _databaseService.EnqueueActionAsync("AddNote", payload, ticketUpdatedAt);
        await NotifyCountChanged();
    }

    public async Task EnqueueCreateTicketAsync(string subject, string? issue, string? typeId, string? priorityId, string? groupId, string? assigneeId)
    {
        var payload = JsonSerializer.Serialize(new { subject, issue, typeId, priorityId, groupId, assigneeId });
        await _databaseService.EnqueueActionAsync("CreateTicket", payload);
        await NotifyCountChanged();
    }

    public async Task EnqueueAssignAsync(string ticketId, string userId, DateTime? ticketUpdatedAt = null)
    {
        var payload = JsonSerializer.Serialize(new { ticketId, userId });
        await _databaseService.EnqueueActionAsync("AssignTicket", payload, ticketUpdatedAt);
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
                // Skip already-conflicted actions (user must resolve manually)
                if (action.IsConflicted) { allSucceeded = false; continue; }

                // Check for conflicts on ticket-related actions
                if (action.TicketUpdatedAt.HasValue && action.ActionType != "CreateTicket")
                {
                    var conflict = await CheckConflictAsync(action);
                    if (conflict != null)
                    {
                        await _databaseService.MarkActionConflictedAsync(action.Id, conflict);
                        action.IsConflicted = true;
                        action.ConflictReason = conflict;
                        MainThread.BeginInvokeOnMainThread(() => ConflictDetected?.Invoke(action));
                        allSucceeded = false;
                        continue;
                    }
                }

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

    /// <summary>Fetches ticket from server and checks if it was modified since the action was queued.</summary>
    private async Task<string?> CheckConflictAsync(Data.PendingAction action)
    {
        try
        {
            var doc = JsonDocument.Parse(action.PayloadJson);
            if (!doc.RootElement.TryGetProperty("ticketId", out var ticketIdEl))
                return null;

            var ticketId = ticketIdEl.GetString();
            if (string.IsNullOrEmpty(ticketId)) return null;

            var json = await _apiService.GetTicketAsync(ticketId);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var ticket = JsonSerializer.Deserialize<Ticket>(json, options);

            if (ticket == null) return "Ticket nicht mehr vorhanden.";

            if (ticket.Updated > action.TicketUpdatedAt.Value.AddSeconds(1))
            {
                var diff = ticket.Updated - action.TicketUpdatedAt.Value;
                var who = ticket.Assignee?.Fullname ?? ticket.Owner?.Fullname ?? "jemand";
                return $"Ticket wurde vor {FormatTimeSpan(diff)} von {who} geändert.";
            }

            return null;
        }
        catch
        {
            return null; // Can't check — proceed without conflict
        }
    }

    /// <summary>Force-applies a conflicted action, ignoring the conflict.</summary>
    public async Task<bool> ForceApplyAsync(int actionId)
    {
        var actions = await _databaseService.GetPendingActionsAsync();
        var action = actions.FirstOrDefault(a => a.Id == actionId);
        if (action == null) return false;

        var success = await ProcessActionAsync(action);
        if (success)
        {
            await _databaseService.RemoveActionAsync(actionId);
            await NotifyCountChanged();
        }
        return success;
    }

    /// <summary>Discards a conflicted action.</summary>
    public async Task DiscardActionAsync(int actionId)
    {
        await _databaseService.RemoveActionAsync(actionId);
        await NotifyCountChanged();
    }

    public async Task<List<Data.PendingAction>> GetConflictedActionsAsync()
    {
        var all = await _databaseService.GetPendingActionsAsync();
        return all.Where(a => a.IsConflicted).ToList();
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays} Tag(en)";
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours} Stunde(n)";
        return $"{(int)ts.TotalMinutes} Minute(n)";
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

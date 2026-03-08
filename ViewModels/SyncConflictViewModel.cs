using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Data;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class SyncConflictViewModel : ObservableObject
{
    private readonly ISyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<PendingAction> _conflicts = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasConflicts;

    private static readonly Dictionary<string, string> ActionTypeNames = new()
    {
        ["AddComment"] = "Kommentar",
        ["AddNote"] = "Notiz",
        ["AssignTicket"] = "Zuweisung",
        ["CreateTicket"] = "Neues Ticket"
    };

    public SyncConflictViewModel(ISyncService syncService)
    {
        _syncService = syncService;
    }

    public static string TranslateActionType(string actionType)
    {
        return ActionTypeNames.TryGetValue(actionType, out var name) ? name : actionType;
    }

    [RelayCommand]
    public async Task LoadConflictsAsync()
    {
        IsLoading = true;
        try
        {
            var conflicted = await _syncService.GetConflictedActionsAsync();
            Conflicts.Clear();
            foreach (var c in conflicted)
                Conflicts.Add(c);
            HasConflicts = Conflicts.Count > 0;
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ForceApplyAsync(PendingAction action)
    {
        var success = await _syncService.ForceApplyAsync(action.Id);
        if (success)
        {
            Conflicts.Remove(action);
            HasConflicts = Conflicts.Count > 0;
        }
    }

    [RelayCommand]
    private async Task DiscardAsync(PendingAction action)
    {
        await _syncService.DiscardActionAsync(action.Id);
        Conflicts.Remove(action);
        HasConflicts = Conflicts.Count > 0;
    }
}

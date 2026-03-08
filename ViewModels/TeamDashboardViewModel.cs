using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Models.Responses;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public class TeamMemberStats
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int AssignedTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public string WorkloadColor => AssignedTickets switch
    {
        > 10 => "#E74C3C",
        > 5 => "#FF9800",
        _ => "#4CAF50"
    };
    public string WorkloadLabel => AssignedTickets switch
    {
        > 10 => "Hoch",
        > 5 => "Mittel",
        _ => "Normal"
    };
}

public partial class TeamDashboardViewModel : ObservableObject
{
    private readonly ITrueDeskApiService _apiService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<TeamMemberStats> _teamMembers = [];

    [ObservableProperty]
    private int _totalTeamMembers;

    [ObservableProperty]
    private int _totalAssigned;

    [ObservableProperty]
    private int _unassignedTickets;

    [ObservableProperty]
    private double _averageWorkload;

    public TeamDashboardViewModel(ITrueDeskApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadTeamDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var usersTask = _apiService.GetUsersAsync();
            var ticketsTask = _apiService.GetTicketsAsync();
            await Task.WhenAll(usersTask, ticketsTask);

            var userResponse = JsonSerializer.Deserialize<GetUserResponse>(usersTask.Result, options);
            var tickets = JsonSerializer.Deserialize<Ticket[]>(ticketsTask.Result, options) ?? [];

            TeamMembers.Clear();

            if (userResponse?.Users != null)
            {
                foreach (var user in userResponse.Users.Where(u => !u.Deleted))
                {
                    var userId = user.Id ?? user.InternalId;
                    var assigned = tickets.Where(t => t.Assignee?.Id == userId).ToList();
                    var open = assigned.Count(t => t.Status?.IsResolved != true);
                    var closed = assigned.Count(t => t.Status?.IsResolved == true);

                    TeamMembers.Add(new TeamMemberStats
                    {
                        Name = user.Fullname ?? user.Username ?? "Unknown",
                        Email = user.Email,
                        AssignedTickets = assigned.Count,
                        OpenTickets = open,
                        ClosedTickets = closed
                    });
                }
            }

            // Sort by assigned tickets descending
            var sorted = TeamMembers.OrderByDescending(m => m.AssignedTickets).ToList();
            TeamMembers.Clear();
            foreach (var m in sorted) TeamMembers.Add(m);

            TotalTeamMembers = TeamMembers.Count;
            TotalAssigned = TeamMembers.Sum(m => m.AssignedTickets);
            UnassignedTickets = tickets.Count(t => t.Assignee == null);
            AverageWorkload = TotalTeamMembers > 0 ? Math.Round((double)TotalAssigned / TotalTeamMembers, 1) : 0;
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }
}

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
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public int AssignedTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public double AvgResponseHours { get; set; }
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
    public string AvgResponseDisplay => AvgResponseHours > 0
        ? $"{AvgResponseHours:F1}h"
        : "-";
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

    // Server-side stats
    [ObservableProperty]
    private int _totalTickets;

    [ObservableProperty]
    private int _closedTickets;

    [ObservableProperty]
    private double _avgResponseTime;

    [ObservableProperty]
    private string _avgResponseDisplay = "-";

    [ObservableProperty]
    private string? _topRequester;

    [ObservableProperty]
    private string? _topAssignee;

    [ObservableProperty]
    private int _overdueCount;

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

            // Load users, overall stats, and overdue tickets in parallel
            var usersTask = _apiService.GetUsersAsync();
            var statsTask = LoadOverallStatsAsync(options);
            var overdueTask = LoadOverdueCountAsync(options);
            await Task.WhenAll(usersTask, statsTask, overdueTask);

            var userResponse = JsonSerializer.Deserialize<GetUserResponse>(usersTask.Result, options);

            TeamMembers.Clear();

            if (userResponse?.Users != null)
            {
                var activeUsers = userResponse.Users.Where(u => !u.Deleted).ToList();

                // Load per-user stats from server in parallel
                var userStatTasks = activeUsers.Select(async user =>
                {
                    var userId = user.Id ?? user.InternalId;
                    if (string.IsNullOrEmpty(userId))
                        return new TeamMemberStats
                        {
                            Name = user.Fullname ?? user.Username ?? "Unknown",
                            Email = user.Email,
                            UserId = userId
                        };

                    try
                    {
                        var statsJson = await _apiService.GetTicketStatsForUserAsync(userId);
                        var userStats = JsonSerializer.Deserialize<UserTicketStats>(statsJson, options);
                        return new TeamMemberStats
                        {
                            Name = user.Fullname ?? user.Username ?? "Unknown",
                            Email = user.Email,
                            UserId = userId,
                            AssignedTickets = userStats?.Data?.TicketCount ?? 0,
                            OpenTickets = (userStats?.Data?.TicketCount ?? 0) - (userStats?.Data?.ClosedCount ?? 0),
                            ClosedTickets = userStats?.Data?.ClosedCount ?? 0,
                            AvgResponseHours = userStats?.Data?.AvgResponse ?? 0
                        };
                    }
                    catch
                    {
                        return new TeamMemberStats
                        {
                            Name = user.Fullname ?? user.Username ?? "Unknown",
                            Email = user.Email,
                            UserId = userId
                        };
                    }
                }).ToList();

                var results = await Task.WhenAll(userStatTasks);

                // Sort by assigned tickets descending, skip users with 0 tickets at the bottom
                var sorted = results
                    .OrderByDescending(m => m.AssignedTickets)
                    .ToList();

                foreach (var m in sorted) TeamMembers.Add(m);
            }

            TotalTeamMembers = TeamMembers.Count;
            TotalAssigned = TeamMembers.Sum(m => m.AssignedTickets);
            AverageWorkload = TotalTeamMembers > 0 ? Math.Round((double)TotalAssigned / TotalTeamMembers, 1) : 0;
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadOverallStatsAsync(JsonSerializerOptions options)
    {
        try
        {
            var json = await _apiService.GetTicketStatsAsync(30);
            // Stats endpoint returns cached data — fields may be null/missing when cache isn't populated
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ticketCount", out var tc) && tc.ValueKind == JsonValueKind.Number)
                TotalTickets = tc.GetInt32();
            if (root.TryGetProperty("closedCount", out var cc) && cc.ValueKind == JsonValueKind.Number)
                ClosedTickets = cc.GetInt32();
            if (root.TryGetProperty("ticketAvg", out var ta) && ta.ValueKind == JsonValueKind.Number)
            {
                AvgResponseTime = ta.GetDouble();
                AvgResponseDisplay = AvgResponseTime > 0 ? $"{AvgResponseTime:F1}h" : "-";
            }
            if (root.TryGetProperty("mostRequester", out var mr) && mr.ValueKind == JsonValueKind.Object)
            {
                var item = JsonSerializer.Deserialize<TicketStatItem>(mr.GetRawText(), options);
                TopRequester = item?.Fullname ?? item?.Name;
            }
            if (root.TryGetProperty("mostAssignee", out var ma) && ma.ValueKind == JsonValueKind.Object)
            {
                var item = JsonSerializer.Deserialize<TicketStatItem>(ma.GetRawText(), options);
                TopAssignee = item?.Fullname ?? item?.Name;
            }
        }
        catch { }
    }

    private async Task LoadOverdueCountAsync(JsonSerializerOptions options)
    {
        try
        {
            var json = await _apiService.GetOverdueTicketsAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tickets", out var ticketsEl) && ticketsEl.ValueKind == JsonValueKind.Array)
                OverdueCount = ticketsEl.GetArrayLength();
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                OverdueCount = doc.RootElement.GetArrayLength();
        }
        catch { }
    }
}

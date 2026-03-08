using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class ReportingViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;

    [ObservableProperty]
    private bool _isLoading;

    // Weekly trend (last 8 weeks)
    [ObservableProperty]
    private ObservableCollection<WeeklyCount> _weeklyTrend = new();

    // Resolution time
    [ObservableProperty]
    private string _avgResolutionTime = "-";

    [ObservableProperty]
    private string _fastestResolution = "-";

    [ObservableProperty]
    private string _slowestResolution = "-";

    [ObservableProperty]
    private int _resolvedCount;

    // SLA
    [ObservableProperty]
    private int _slaMetCount;

    [ObservableProperty]
    private int _slaBreachedCount;

    [ObservableProperty]
    private int _slaAtRiskCount;

    [ObservableProperty]
    private int _slaNoDeadlineCount;

    [ObservableProperty]
    private double _slaCompliancePercent;

    [ObservableProperty]
    private string _slaComplianceText = "-";

    // Priority distribution
    [ObservableProperty]
    private ObservableCollection<PriorityStats> _priorityDistribution = new();

    // Top assignees
    [ObservableProperty]
    private ObservableCollection<AssigneeStats> _topAssignees = new();

    public ReportingViewModel(TrueDeskApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadReportAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var json = await _apiService.GetTicketsAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tickets = JsonSerializer.Deserialize<Ticket[]>(json, options) ?? [];

            foreach (var t in tickets)
                TrudeskTranslationHelper.TranslateTicket(t);

            ComputeWeeklyTrend(tickets);
            ComputeResolutionTime(tickets);
            ComputeSlaCompliance(tickets);
            ComputePriorityDistribution(tickets);
            ComputeTopAssignees(tickets);
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeWeeklyTrend(Ticket[] tickets)
    {
        WeeklyTrend.Clear();
        var now = DateTime.Now;
        var weeks = new List<WeeklyCount>();

        for (int i = 7; i >= 0; i--)
        {
            var weekStart = now.Date.AddDays(-((int)now.DayOfWeek + 7 * i));
            var weekEnd = weekStart.AddDays(7);
            var created = tickets.Count(t => t.Date >= weekStart && t.Date < weekEnd);
            var closed = tickets.Count(t => t.ClosedDate.HasValue && t.ClosedDate.Value >= weekStart && t.ClosedDate.Value < weekEnd);
            weeks.Add(new WeeklyCount(weekStart.ToString("dd.MM"), created, closed));
        }

        var maxVal = weeks.Max(w => Math.Max(w.Created, w.Closed));
        foreach (var w in weeks)
        {
            w.MaxValue = maxVal > 0 ? maxVal : 1;
            WeeklyTrend.Add(w);
        }
    }

    private void ComputeResolutionTime(Ticket[] tickets)
    {
        var resolved = tickets
            .Where(t => t.ClosedDate.HasValue && t.ClosedDate.Value > t.Date)
            .Select(t => t.ClosedDate!.Value - t.Date)
            .ToList();

        ResolvedCount = resolved.Count;

        if (resolved.Count == 0)
        {
            AvgResolutionTime = "-";
            FastestResolution = "-";
            SlowestResolution = "-";
            return;
        }

        var avg = TimeSpan.FromTicks((long)resolved.Average(ts => ts.Ticks));
        AvgResolutionTime = FormatDuration(avg);
        FastestResolution = FormatDuration(resolved.Min());
        SlowestResolution = FormatDuration(resolved.Max());
    }

    private void ComputeSlaCompliance(Ticket[] tickets)
    {
        var withDeadline = tickets.Where(t => t.DueDate != DateTime.MinValue).ToList();
        var noDeadline = tickets.Length - withDeadline.Count;
        SlaNoDeadlineCount = noDeadline;

        if (withDeadline.Count == 0)
        {
            SlaMetCount = 0;
            SlaBreachedCount = 0;
            SlaAtRiskCount = 0;
            SlaCompliancePercent = 0;
            SlaComplianceText = "Keine Tickets mit Fälligkeitsdatum";
            return;
        }

        var met = 0;
        var breached = 0;
        var atRisk = 0;

        foreach (var t in withDeadline)
        {
            if (t.Status?.IsResolved == true)
            {
                if (t.ClosedDate.HasValue && t.ClosedDate.Value <= t.DueDate)
                    met++;
                else
                    breached++;
            }
            else
            {
                if (t.DueDate < DateTime.Now)
                    breached++;
                else if (t.DueDate < DateTime.Now.AddHours(24))
                    atRisk++;
                else
                    met++; // Still within SLA
            }
        }

        SlaMetCount = met;
        SlaBreachedCount = breached;
        SlaAtRiskCount = atRisk;

        var total = met + breached + atRisk;
        SlaCompliancePercent = total > 0 ? Math.Round(met * 100.0 / total, 1) : 0;
        SlaComplianceText = $"{SlaCompliancePercent}% SLA-Einhaltung";
    }

    private void ComputePriorityDistribution(Ticket[] tickets)
    {
        PriorityDistribution.Clear();
        var groups = tickets
            .Where(t => t.Priority != null)
            .GroupBy(t => new { t.Priority!.Name, t.Priority.HtmlColor })
            .OrderByDescending(g => g.Count());

        var maxCount = groups.Any() ? groups.Max(g => g.Count()) : 1;

        foreach (var g in groups)
        {
            PriorityDistribution.Add(new PriorityStats(
                g.Key.Name ?? "Unbekannt",
                g.Count(),
                g.Key.HtmlColor ?? "#9E9E9E",
                maxCount));
        }
    }

    private void ComputeTopAssignees(Ticket[] tickets)
    {
        TopAssignees.Clear();
        var groups = tickets
            .Where(t => t.Assignee != null && !string.IsNullOrEmpty(t.Assignee.Fullname))
            .GroupBy(t => t.Assignee!.Fullname)
            .OrderByDescending(g => g.Count())
            .Take(5);

        var maxCount = groups.Any() ? groups.Max(g => g.Count()) : 1;

        foreach (var g in groups)
        {
            var open = g.Count(t => t.Status?.IsResolved != true);
            var closed = g.Count(t => t.Status?.IsResolved == true);
            TopAssignees.Add(new AssigneeStats(g.Key!, g.Count(), open, closed, maxCount));
        }
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}T {ts.Hours}h";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m";
    }
}

public class WeeklyCount
{
    public string WeekLabel { get; set; }
    public int Created { get; set; }
    public int Closed { get; set; }
    public int MaxValue { get; set; } = 1;
    public double CreatedBarWidth => MaxValue > 0 ? Created * 200.0 / MaxValue : 0;
    public double ClosedBarWidth => MaxValue > 0 ? Closed * 200.0 / MaxValue : 0;

    public WeeklyCount(string weekLabel, int created, int closed)
    {
        WeekLabel = weekLabel;
        Created = created;
        Closed = closed;
    }
}

public class PriorityStats
{
    public string Name { get; set; }
    public int Count { get; set; }
    public string Color { get; set; }
    public double BarWidth { get; set; }

    public PriorityStats(string name, int count, string color, int maxCount)
    {
        Name = name;
        Count = count;
        Color = color;
        BarWidth = maxCount > 0 ? count * 200.0 / maxCount : 0;
    }
}

public class AssigneeStats
{
    public string Name { get; set; }
    public int Total { get; set; }
    public int Open { get; set; }
    public int Closed { get; set; }
    public double BarWidth { get; set; }

    public AssigneeStats(string name, int total, int open, int closed, int maxCount)
    {
        Name = name;
        Total = total;
        Open = open;
        Closed = closed;
        BarWidth = maxCount > 0 ? total * 200.0 / maxCount : 0;
    }
}

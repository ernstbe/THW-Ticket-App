using THWTicketApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{
    private readonly TrueDeskApiService _apiService;
    public ObservableCollection<Ticket> Tickets { get; set; } = new();

    public TicketPage(TrueDeskApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        TicketsCollectionView.ItemsSource = this.Tickets;
        LoadTickets();
    }

    private async void LoadTickets()
    {
        try
        {
            StatusLabel.Text = "Loading tickets...";
            var json = await _apiService.GetTicketsAsync();
            Tickets.Clear();
            var tickets = JsonSerializer.Deserialize<List<Ticket>>(json);
            if (tickets == null || tickets.Count() == 0)
            {
                StatusLabel.Text = "No tickets found.";
                return;
            }
            foreach (var ticket in tickets)
            {
                Tickets.Add(ticket);
            }
            StatusLabel.Text = "";
         }
        catch (System.Exception ex )
        {
            Console.WriteLine($"Error loading tickets: {ex.Message}");
        }
    }
    
    private void OnRefreshClicked(object sender, EventArgs e) => LoadTickets();

    private async void OnAddTicketClicked(object sender, EventArgs e)
    {
        var title = NewTicketTitleEntry.Text;
        var desc = NewTicketDescEntry.Text;
        if (int.TryParse(AssignUserIdEntry.Text, out int userId))
        {
            var result = await _apiService.AddTicketAsync(title, desc, userId);
            StatusLabel.Text = "Ticket added.";
            LoadTickets();
        }
        else
        {
            StatusLabel.Text = "Invalid User ID.";
        }
    }

    private async void OnAssignClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int ticketId)
        {
            if (int.TryParse(AssignUserIdEntry.Text, out int userId))
            {
                var result = await _apiService.AssignTicketAsync(ticketId, userId);
                StatusLabel.Text = "Ticket assigned.";
                LoadTickets();
            }
            else
            {
                StatusLabel.Text = "Invalid User ID.";
            }
        }
    }
}

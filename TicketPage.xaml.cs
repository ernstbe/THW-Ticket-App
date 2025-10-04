using THWTicketApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{
    private readonly TrueDeskApiService _apiService;
    public ObservableCollection<TicketModel> Tickets { get; set; } = new();

    public TicketPage(TrueDeskApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        TicketsCollectionView.ItemsSource = Tickets;
        LoadTickets();
    }

    private async void LoadTickets()
    {
        try
        {
            StatusLabel.Text = "Loading tickets...";
            var json = await _apiService.GetTicketsAsync();
            Tickets.Clear();
            var tickets = JsonSerializer.Deserialize<TicketModel[]>(json);
            if (tickets == null || tickets.Length == 0)
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

    public class TicketModel
    {
        public string Id { get; set; }
        public GroupModel Group { get; set; }
        public bool Deleted { get; set; }
        public TypeModel Type { get; set; }
        public PriorityModel Priority { get; set; }
        public List<string> Tags { get; set; }
        public string Subject { get; set; }
        public string Issue { get; set; }
        public List<string> Subscribers { get; set; }
        public string Date { get; set; }
        public List<CommentModel> Comments { get; set; }
        public List<NoteModel> Notes { get; set; }
        public List<AttachmentModel> Attachments { get; set; }
        public List<HistoryModel> History { get; set; }
        public StatusModel Status { get; set; }
        public UserModel Owner { get; set; }
        public int Uid { get; set; }
        public int __v { get; set; }
        public UserModel Assignee { get; set; }
        public string ClosedDate { get; set; }
    }

    public class GroupModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TypeModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<PriorityModel> Priorities { get; set; }
    }

    public class PriorityModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int OverdueIn { get; set; }
        public string HtmlColor { get; set; }
        public int MigrationNum { get; set; }
        public bool Default { get; set; }
        public int __v { get; set; }
        public string DurationFormatted { get; set; }
    }

    public class CommentModel { }
    public class NoteModel { }
    public class AttachmentModel { }

    public class HistoryModel
    {
        public string Action { get; set; }
        public string Date { get; set; }
        public UserModel Owner { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
    }

    public class StatusModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string HtmlColor { get; set; }
        public int Uid { get; set; }
        public int Order { get; set; }
        public bool Slatimer { get; set; }
        public bool IsResolved { get; set; }
        public bool IsLocked { get; set; }
        public int __v { get; set; }
    }

    public class UserModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Role { get; set; }
        public RoleModel RoleObj { get; set; }
    }

    public class RoleModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Normalized { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsAgent { get; set; }
    }
}

using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class TicketPageViewModel : ObservableObject
    {

        [ObservableProperty]
        public string statusMessage = string.Empty;

        [ObservableProperty]
        public ObservableCollection<Ticket> tickets = new();

        public AsyncRelayCommand RefreshTicketsAsync { get; }
        public AsyncRelayCommand LoadTicketsAsync { get; }
        public AsyncRelayCommand<Tuple<string, string, int>> AddTicketAsync { get; }

    private readonly TrueDeskApiService _apiService;
    public TrueDeskApiService ApiService => _apiService;


        public TicketPageViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
            RefreshTicketsAsync = new AsyncRelayCommand(() => LoadTickets());
            LoadTicketsAsync = new AsyncRelayCommand(() => LoadTickets());
            AddTicketAsync = new AsyncRelayCommand<Tuple<string, string, int>>((data) => AddTicket(data));

            // Subscribe to ticket updates via NotificationCenter
            THWTicketApp.Utils.NotificationCenter.TicketUpdated += (ticketId) =>
            {
                _ = LoadTickets();
            };
        }


        public async Task LoadTickets()
        {
            try
            {
                StatusMessage = "Loading tickets...";
                var json = await _apiService.GetTicketsAsync();
                Tickets.Clear();
                Console.WriteLine(json);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var tickets = JsonSerializer.Deserialize<Ticket[]>(json, options);
                if (tickets == null || tickets.Count() == 0)
                {
                    StatusMessage = "No tickets found.";
                    return;
                }

                foreach (var ticket in tickets)
                {
                    Tickets.Add(ticket);
                }

                StatusMessage = "";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                throw;

            }
        }

        private async Task AddTicket(Tuple<string, string, int>? tuple)
        {
            if (tuple != null)
            {
                var result = await _apiService.AddTicketAsync(tuple.Item1, tuple.Item2, tuple.Item3);
                StatusMessage = "Ticket added.";
                await LoadTickets();
            }
            else
            {
                StatusMessage = "Invalid input.";
            }
        }
    }
}

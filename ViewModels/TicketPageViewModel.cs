using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels
{
    public partial class TicketPageViewModel : ObservableObject
    {
        private readonly TrueDeskApiService _apiService;
        [ObservableProperty]
        private string statusMessage = string.Empty;
        [ObservableProperty]
        private ObservableCollection<Ticket> tickets = new();

        public TicketPageViewModel(TrueDeskApiService apiService)
        {
            _apiService = apiService;
            Task.Run(async () => await LoadTicketsAsync());
        }



        // Removed: event handler not needed in ViewModel

    [RelayCommand]
    public async Task LoadTicketsAsync()
        {
            try
            {
                StatusMessage = "Loading tickets...";
                var json = await _apiService.GetTicketsAsync();
                Tickets.Clear();
                var tickets = System.Text.Json.JsonSerializer.Deserialize<List<Ticket>>(json);
                if (tickets == null || tickets.Count == 0)
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
            }
        }

        [RelayCommand]
        public async Task AddTicketAsync(Tuple<string, string, int> tuple)
        {
            if (tuple != null)
            {
                var result = await _apiService.AddTicketAsync(tuple.Item1, tuple.Item2, tuple.Item3);
                StatusMessage = "Ticket added.";
                await LoadTicketsAsync();
            }
            else
            {
                StatusMessage = "Invalid input.";
            }
        }
    }
}


using Microsoft.Maui.Controls;
using THWTicketApp.Models;
using THWTicketApp.ViewModels;
using THWTicketApp.Services;

namespace THWTicketApp.Views
{
    public partial class TicketDetailPage : ContentPage
    {
        private readonly TicketDetailViewModel _viewModel;

        public TicketDetailPage(TicketDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        public void SetTicket(Ticket ticket)
        {
            _viewModel.SetTicket(ticket);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadUsersAsync();
        }
    }
}

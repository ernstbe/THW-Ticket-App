using Microsoft.Maui.Controls;
using THWTicketApp.Models;
using THWTicketApp.ViewModels;

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
            await Task.WhenAll(
                _viewModel.LoadUsersAsync(),
                _viewModel.LoadStatusesAsync()
            );
        }
    }
}

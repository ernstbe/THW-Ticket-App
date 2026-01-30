using Microsoft.Maui.Controls;
using THWTicketApp.Models;
using THWTicketApp.ViewModels;
<<<<<<< HEAD
using THWTicketApp.Services;
=======
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

namespace THWTicketApp.Views
{
    public partial class TicketDetailPage : ContentPage
    {
        private readonly TicketDetailViewModel _viewModel;
<<<<<<< HEAD
        public TicketDetailPage(Ticket ticket, TrueDeskApiService apiService)
        {

            InitializeComponent();
            _viewModel = new TicketDetailViewModel(ticket, apiService);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadUsers();
=======

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
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }
    }
}

using Microsoft.Maui.Controls;
using THWTicketApp.Models;
using THWTicketApp.ViewModels;
using THWTicketApp.Services;

namespace THWTicketApp.Views
{
    public partial class TicketDetailPage : ContentPage
    {
        private readonly TicketDetailViewModel _viewModel;
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
        }
    }
}

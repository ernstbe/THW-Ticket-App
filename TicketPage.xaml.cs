using THWTicketApp.Services;
using System;
using Microsoft.Maui.Controls;

namespace THWTicketApp;

public partial class TicketPage : ContentPage
{
    private readonly TrueDeskApiService _apiService;
    private readonly ViewModels.TicketPageViewModel _viewModel;

    public TicketPage(TrueDeskApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        _viewModel = new ViewModels.TicketPageViewModel(_apiService);
        BindingContext = _viewModel;
    }
}

<<<<<<< HEAD
﻿using THWTicketApp.ViewModels;
=======
using THWTicketApp.ViewModels;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

namespace THWTicketApp;

public partial class MainPage : ContentPage
{
<<<<<<< HEAD

    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
=======
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
    }
}

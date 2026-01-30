using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class AddTicketPage : ContentPage
{
    public AddTicketPage(AddTicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AddTicketViewModel vm)
        {
            await vm.LoadDataAsync();
        }
    }
}

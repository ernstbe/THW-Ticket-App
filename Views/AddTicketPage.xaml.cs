using THWTicketApp.ViewModels;

namespace THWTicketApp.Views;

public partial class AddTicketPage : ContentPage
{
    public AddTicketPage(AddTicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void SetScannedSubject(string subject)
    {
        if (BindingContext is AddTicketViewModel vm)
        {
            vm.Subject = $"[Scan] {subject}";
        }
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

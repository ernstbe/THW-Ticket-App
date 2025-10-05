using THWTicketApp.Services;

namespace THWTicketApp;

public partial class LoginPage : ContentPage
{
    private readonly TrueDeskApiService _apiService = new TrueDeskApiService();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginStatusLabel.Text = "Logging in...";
        var username = UsernameEntry.Text;
        username= "ErnstBe";
        var password = PasswordEntry.Text;
        password= "Darkben123";
        var success = await _apiService.AuthenticateAsync(username, password);
        if (success)
        {
            LoginStatusLabel.Text = "Login successful!";
            // Navigate to MainPage or TicketPage
            Application.Current.MainPage = new TicketPage(_apiService);
        }
        else
        {
            LoginStatusLabel.Text = "Login failed. Check credentials.";
        }
    }
}

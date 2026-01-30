namespace THWTicketApp;

public partial class App : Application
{
	private readonly LoginPage _loginPage;
	public App(LoginPage loginPage)
	{
		InitializeComponent();
		_loginPage = loginPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Start with LoginPage wrapped in NavigationPage
		return new Window(new NavigationPage(_loginPage));
	}
}
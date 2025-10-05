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
		// Start with LoginPage
		return new Window(_loginPage);
	}
}
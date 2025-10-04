namespace THWTicketApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Start with LoginPage
		return new Window(new LoginPage());
	}
}
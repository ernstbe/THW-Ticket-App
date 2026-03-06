namespace THWTicketApp;

public partial class App : Application
{
	private readonly LoginPage _loginPage;
	private readonly Services.NotificationService _notificationService;

	public App(LoginPage loginPage, Services.NotificationService notificationService)
	{
		InitializeComponent();
		_loginPage = loginPage;
		_notificationService = notificationService;

		// Restore saved theme preference
		var isDarkMode = Preferences.Get("DarkMode", false);
		UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Start with LoginPage wrapped in NavigationPage
		return new Window(new NavigationPage(_loginPage));
	}
}
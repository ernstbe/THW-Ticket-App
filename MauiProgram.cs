using Microsoft.Extensions.Logging;
using THWTicketApp.Services;
using THWTicketApp.ViewModels;
using THWTicketApp.Views;

namespace THWTicketApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Services.AddSingleton<TrueDeskApiService>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddSingleton<LoginPage>();
		builder.Services.AddSingleton<TicketPageViewModel>();
		builder.Services.AddSingleton<TicketPage>();



#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

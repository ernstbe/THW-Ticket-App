<<<<<<< HEAD
﻿using Microsoft.Extensions.Logging;
using THWTicketApp.Services;
using THWTicketApp.ViewModels;
using THWTicketApp.Views;
=======
using Microsoft.Extensions.Logging;
using THWTicketApp.Services;
using THWTicketApp.ViewModels;
using THWTicketApp.Views;
using THWTicketApp.Converters;
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)

namespace THWTicketApp;

public static class MauiProgram
{
<<<<<<< HEAD
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
=======
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

        // Configuration
        var appSettings = new AppSettings
        {
            // Configure API URL here or load from configuration
            ApiBaseUrl = "http://localhost:8118/api/v1",
            ConnectionTimeoutSeconds = 30
        };
        builder.Services.AddSingleton(appSettings);

        // Services
        builder.Services.AddSingleton<TrueDeskApiService>();
        builder.Services.AddSingleton<DatabaseService>();

        // ViewModels
        builder.Services.AddSingleton<LoginPageViewModel>();
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<TicketPageViewModel>();
        builder.Services.AddTransient<TicketDetailViewModel>();
        builder.Services.AddTransient<AddTicketViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<TicketPage>();
        builder.Services.AddTransient<TicketDetailPage>();
        builder.Services.AddTransient<Views.AddTicketPage>();
        builder.Services.AddTransient<Views.SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
}

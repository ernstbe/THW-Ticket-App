using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Data;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class NotificationHistoryViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<NotificationEntry> _notifications = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNotifications;

    public NotificationHistoryViewModel(IDatabaseService databaseService, IServiceProvider serviceProvider)
    {
        _databaseService = databaseService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    public async Task LoadNotificationsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var entries = await _databaseService.GetNotificationsAsync();
            Notifications.Clear();
            foreach (var entry in entries)
                Notifications.Add(entry);

            HasNotifications = Notifications.Count > 0;

            await _databaseService.MarkAllNotificationsReadAsync();
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenTicketAsync(NotificationEntry entry)
    {
        if (string.IsNullOrEmpty(entry.TicketId)) return;

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is NavigationPage nav)
        {
            var detailPage = _serviceProvider.GetRequiredService<Views.TicketDetailPage>();
            detailPage.SetTicket(new Models.Ticket { Id = entry.TicketId });
            await nav.PushAsync(detailPage);
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _databaseService.ClearNotificationHistoryAsync();
        Notifications.Clear();
        HasNotifications = false;
    }
}

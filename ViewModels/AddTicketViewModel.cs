using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using THWTicketApp.Models;
using THWTicketApp.Services;

namespace THWTicketApp.ViewModels;

public partial class AddTicketViewModel : ObservableObject
{
    private readonly TrueDeskApiService _apiService;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _issue = string.Empty;

    [ObservableProperty]
    private TicketType? _selectedType;

    [ObservableProperty]
    private Priority? _selectedPriority;

    [ObservableProperty]
    private Group? _selectedGroup;

    [ObservableProperty]
    private User? _selectedAssignee;

    [ObservableProperty]
    private ObservableCollection<TicketType> _ticketTypes = [];

    [ObservableProperty]
    private ObservableCollection<Priority> _priorities = [];

    [ObservableProperty]
    private ObservableCollection<Group> _groups = [];

    [ObservableProperty]
    private ObservableCollection<User> _users = [];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Color _statusColor = Colors.Red;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public bool CanCreate => !string.IsNullOrWhiteSpace(Subject) && SelectedGroup != null && !IsLoading;

    partial void OnSubjectChanged(string value) => OnPropertyChanged(nameof(CanCreate));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanCreate));
    partial void OnSelectedGroupChanged(Group? value) => OnPropertyChanged(nameof(CanCreate));

    public AddTicketViewModel(TrueDeskApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(
                LoadUsersAsync(),
                LoadTypesAsync(),
                LoadGroupsAsync()
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var json = await _apiService.GetUsersAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = System.Text.Json.JsonSerializer.Deserialize<Models.Responses.GetUserResponse>(json, options);

            Users.Clear();
            if (response?.Users != null)
            {
                foreach (var user in response.Users)
                {
                    Users.Add(user);
                }
            }
        }
        catch
        {
            // Silently fail - users are optional
        }
    }

    private async Task LoadTypesAsync()
    {
        try
        {
            var json = await _apiService.GetTicketTypesAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var types = System.Text.Json.JsonSerializer.Deserialize<TicketType[]>(json, options);

            TicketTypes.Clear();
            if (types != null)
            {
                foreach (var type in types)
                {
                    TicketTypes.Add(type);
                    // Add priorities from this type
                    if (type.Priorities != null)
                    {
                        foreach (var priority in type.Priorities)
                        {
                            if (!Priorities.Any(p => p.Id == priority.Id))
                            {
                                Priorities.Add(priority);
                            }
                        }
                    }
                }
                if (TicketTypes.Count > 0)
                {
                    SelectedType = TicketTypes[0];
                }
            }
        }
        catch
        {
            // Use default priorities if types fail to load
        }
    }

    private async Task LoadGroupsAsync()
    {
        try
        {
            var json = await _apiService.GetGroupsAsync();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = System.Text.Json.JsonSerializer.Deserialize<Models.Responses.GetGroupResponse>(json, options);

            Groups.Clear();
            if (response?.Groups != null)
            {
                foreach (var group in response.Groups)
                {
                    Groups.Add(group);
                }
                if (Groups.Count > 0)
                {
                    SelectedGroup = Groups[0];
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }

    [RelayCommand]
    private async Task CreateTicketAsync()
    {
        if (string.IsNullOrWhiteSpace(Subject))
        {
            StatusMessage = "Bitte geben Sie einen Betreff ein.";
            StatusColor = Colors.Red;
            OnPropertyChanged(nameof(HasStatusMessage));
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(HasStatusMessage));
        OnPropertyChanged(nameof(CanCreate));

        try
        {
            var success = await _apiService.CreateTicketAsync(
                Subject,
                Issue,
                SelectedType?.Id,
                SelectedPriority?.Id,
                SelectedGroup?.Id,
                SelectedAssignee?.Id
            );

            if (success)
            {
                StatusMessage = "Ticket erfolgreich erstellt!";
                StatusColor = Colors.Green;
                OnPropertyChanged(nameof(HasStatusMessage));

                // Navigate back after short delay
                await Task.Delay(1000);
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window?.Page is NavigationPage nav)
                {
                    await nav.Navigation.PopAsync();
                }
            }
            else
            {
                StatusMessage = "Fehler beim Erstellen des Tickets.";
                StatusColor = Colors.Red;
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
        catch (Exception)
        {
            StatusMessage = "Verbindungsfehler. Bitte versuchen Sie es erneut.";
            StatusColor = Colors.Red;
            OnPropertyChanged(nameof(HasStatusMessage));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is NavigationPage nav)
        {
            await nav.Navigation.PopAsync();
        }
    }
}

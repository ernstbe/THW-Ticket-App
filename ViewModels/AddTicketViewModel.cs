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
    [NotifyPropertyChangedFor(nameof(CanCreate))]
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
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Color _statusColor = Colors.Red;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private bool _isLoading;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public bool CanCreate => !string.IsNullOrWhiteSpace(Subject) && !IsLoading;

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
                    // Add priorities from this type (translate names)
                    if (type.Priorities != null)
                    {
                        foreach (var priority in type.Priorities)
                        {
                            if (!Priorities.Any(p => p.Id == priority.Id))
                            {
                                priority.Name = TrudeskTranslationHelper.TranslatePriority(priority.Name);
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
            var groups = System.Text.Json.JsonSerializer.Deserialize<Group[]>(json, options);

            Groups.Clear();
            if (groups != null)
            {
                foreach (var group in groups)
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
            StatusMessage = "Bitte gib einen gültigen Betreff ein.";
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
                StatusMessage = "Erfolg! Ticket erstellt.";
                StatusColor = Colors.Green;
                OnPropertyChanged(nameof(HasStatusMessage));

                // Navigate back after short delay
                await Task.Delay(1000);
                await Shell.Current.GoToAsync("..");
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
            StatusMessage = "Netzwerkfehler. Bitte erneut versuchen.";
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

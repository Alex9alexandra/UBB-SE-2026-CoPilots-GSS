using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.ViewModels;

public partial class CreateEventViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IQuestService _questService;
    private readonly IAttendedEventService _attendedEventService;

    public CreateEventViewModel(IUserService userService, IEventService eventService, IQuestService questService, IAttendedEventService attendedEventService)
    {
        _userService = userService;
        _eventService = eventService;
        _questService = questService;
        _attendedEventService = attendedEventService;
    }

    //VM1

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep2Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep3Visible))]
    public partial int CurrentStep { get; set; } = 1;

    public Visibility IsStep1Visible => CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsStep2Visible => CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsStep3Visible => CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial string EventName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial TimeSpan StartTime { get; set; } = DateTime.Now.TimeOfDay;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial DateTimeOffset EndDate { get; set; } = DateTimeOffset.Now.AddDays(1);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial TimeSpan EndTime { get; set; } = DateTime.Now.AddHours(1).TimeOfDay;

    [ObservableProperty]
    public partial bool IsPublic { get; set; } = true;

    //VALIDATION PART

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;

    //VM 2

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MaximumPeopleText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBannerImage))]
    [NotifyPropertyChangedFor(nameof(BannerImageVisibility))]
    public partial string? EventBannerPath { get; set; }

    public bool HasBannerImage => !string.IsNullOrEmpty(EventBannerPath);
    public Visibility BannerImageVisibility => HasBannerImage ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    public ObservableCollection<Category> AvailableCategories { get; } = new()
    {
        new Category { CategoryId = 1, Title = "NATURE" },
        new Category { CategoryId = 2, Title = "FITNESS" },
        new Category { CategoryId = 3, Title = "MUSIC" },
        new Category { CategoryId = 4, Title = "SOCIAL" },
        new Category { CategoryId = 5, Title = "ART" },
        new Category { CategoryId = 6, Title = "PETS" },
        new Category { CategoryId = 7, Title = "TECH" },
        new Category { CategoryId = 8, Title = "FUN" },
    };

    //VM3

    public ObservableCollection<Quest> AvailableQuests { get; } = new();
    public ObservableCollection<Quest> SelectedQuests { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string CustomQuestName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string CustomQuestDescription { get; set; } = string.Empty;

public event Action<CreateEventDto?>? CloseRequested;

    //Commands

    [RelayCommand(CanExecute = nameof(CanGoToStep2))]
    private void GoToStep2()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(EventName))
        {
            ErrorMessage = "Event name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            ErrorMessage = "Location is required.";
            return;
        }

        var start = StartDate.Date + StartTime;
        var end = EndDate.Date + EndTime;

        if (end <= start)
        {
            ErrorMessage = "End date/time must be after start date/time.";
            return;
        }

        CurrentStep = 2;
    }

    private bool CanGoToStep2() =>
        !string.IsNullOrWhiteSpace(EventName) &&
        !string.IsNullOrWhiteSpace(Location);

    [RelayCommand]
    private void GoToStep3()
    {
        CurrentStep = 3;
    }

    [RelayCommand]
    private void GoBackToStep1()
    {
        CurrentStep = 1;
    }

    [RelayCommand]
    private void GoBackToStep2()
    {
        CurrentStep = 2;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(null);
    }

    [RelayCommand(CanExecute = nameof(CanAddCustomQuest))]
    private void AddCustomQuest()
    {
        var quest = new Quest
        {
            Name = CustomQuestName.Trim(),
            Description = CustomQuestDescription.Trim(),
            Difficulty = 3,
        };

        SelectedQuests.Add(quest);
        CustomQuestName = string.Empty;
        CustomQuestDescription = string.Empty;
    }

    private bool CanAddCustomQuest() =>
        !string.IsNullOrWhiteSpace(CustomQuestName) &&
        !string.IsNullOrWhiteSpace(CustomQuestDescription);


    [RelayCommand]
    private void ToggleQuestSelection(Quest quest)
    {
        if (SelectedQuests.Contains(quest))
        {
            SelectedQuests.Remove(quest);
        }
        else
        {
            SelectedQuests.Add(quest);
        }
    }

    [RelayCommand]
    private void RemoveQuest(Quest quest)
    {
        if (SelectedQuests.Contains(quest))
        {
            SelectedQuests.Remove(quest);
        }
    }


    [RelayCommand]
    private async System.Threading.Tasks.Task CreateEvent()
    {
        var dto = BuildDto();
        var eventEntity = new Event
        {
            Name = dto.Name,
            Location = dto.Location,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            IsPublic = dto.IsPublic,
            Description = dto.Description,
            MaximumPeople = dto.MaximumPeople,
            EventBannerPath = dto.EventBannerPath,
            Category = dto.Category,
            Admin = dto.Admin!,
        };
        int newEventId = await _eventService.CreateEventAsync(eventEntity);
        eventEntity.EventId = newEventId;
        await _attendedEventService.AttendEventAsync(newEventId, _userService.GetCurrentUser().UserId);

        foreach (var quest in dto.SelectedQuests)
        {
            await _questService.AddQuestAsync(eventEntity, quest);
        }

        CloseRequested?.Invoke(dto);
    }

    public CreateEventDto BuildDto()
    {
        int? maxPeople = int.TryParse(MaximumPeopleText, out var parsed) ? parsed : null;

        return new CreateEventDto
        {
            Name = EventName.Trim(),
            Location = Location.Trim(),
            StartDateTime = StartDate.Date + StartTime,
            EndDateTime = EndDate.Date + EndTime,
            IsPublic = IsPublic,
            Description = string.IsNullOrWhiteSpace(Description) ? "No description yet" : Description.Trim(),
            MaximumPeople = maxPeople,
            EventBannerPath = EventBannerPath,
            Category = SelectedCategory,
            Admin = _userService.GetCurrentUser(),
            SelectedQuests = new List<Quest>(SelectedQuests),
        };
    }

    public async System.Threading.Tasks.Task LoadPresetQuestsAsync(IQuestService questService)
    {
        var quests = await questService.GetPresetQuestsAsync();
        AvailableQuests.Clear();
        foreach (var quest in quests)
        {
            AvailableQuests.Add(quest);
        }
    }

    public bool IsQuestSelected(Quest quest)
    {
        return SelectedQuests.Contains(quest);
    }
}

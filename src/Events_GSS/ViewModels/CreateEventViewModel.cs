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

/// <summary>
/// ViewModel for creating a new event.
/// </summary>
public partial class CreateEventViewModel : ObservableObject
{
    private readonly IUserService userService;
    private readonly IEventService eventService;
    private readonly IQuestService questService;
    private readonly IAttendedEventService attendedEventService;

    public CreateEventViewModel(IUserService userService, IEventService eventService, IQuestService questService, IAttendedEventService attendedEventService)
    {
        this.userService = userService;
        this.eventService = eventService;
        this.questService = questService;
        this.attendedEventService = attendedEventService;
    }

    //VM1

    /// <summary>
    /// Gets or sets the current step in the event creation wizard.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep2Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep3Visible))]
    public partial int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets the event creation details text.
    /// </summary>
    [ObservableProperty]
    public partial string EventCreationDetailsText { get; set; } = string.Empty;

    public Visibility IsStep1Visible => this.CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsStep2Visible => this.CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsStep3Visible => this.CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the start time of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial TimeSpan StartTime { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>
    /// Gets or sets the end date of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial DateTimeOffset EndDate { get; set; } = DateTimeOffset.Now.AddDays(1);

    /// <summary>
    /// Gets or sets the end time of the event.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    public partial TimeSpan EndTime { get; set; } = DateTime.Now.AddHours(1).TimeOfDay;

    /// <summary>
    /// Gets or sets a value indicating whether the event is public.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPublic { get; set; } = true;

    // VALIDATION PART
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => this.ErrorMessage is not null;

    public Visibility ErrorVisibility => this.HasError ? Visibility.Visible : Visibility.Collapsed;

    // VM 2
    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of people as text.
    /// </summary>
    [ObservableProperty]
    public partial string MaximumPeopleText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event banner path.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBannerImage))]
    [NotifyPropertyChangedFor(nameof(BannerImageVisibility))]
    public partial string? EventBannerPath { get; set; }

    public bool HasBannerImage => !string.IsNullOrEmpty(this.EventBannerPath);

    public Visibility BannerImageVisibility => this.HasBannerImage ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the selected category for the event.
    /// </summary>
    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    public ObservableCollection<Category> AvailableCategories { get; } = new ()
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

    // VM3
    public ObservableCollection<Quest> AvailableQuests { get; } = new ();

    public ObservableCollection<Quest> SelectedQuests { get; } = new ();

    /// <summary>
    /// Gets or sets the custom quest name.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string CustomQuestName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom quest description.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    public partial string CustomQuestDescription { get; set; } = string.Empty;

    public event Action<CreateEventDto?>? CloseRequested;

    // Commands
    [RelayCommand(CanExecute = nameof(CanGoToStep2))]
    private void GoToStep2()
    {
        this.ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(this.EventName))
        {
            this.ErrorMessage = "Event name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(this.Location))
        {
            this.ErrorMessage = "Location is required.";
            return;
        }

        var start = this.StartDate.Date + this.StartTime;
        var end = this.EndDate.Date + this.EndTime;

        if (end <= start)
        {
            this.ErrorMessage = "End date/time must be after start date/time.";
            return;
        }

        this.CurrentStep = 2;
    }

    public string BuildEventCreationDetailsText(CreateEventDto? createEventDto)
    {
        if (createEventDto == null)
        {
            return "Event creation cancelled.";
        }

        string selectedQuestNamesText =
            createEventDto.SelectedQuests.Count == 0
                ? "None"
                : string.Join(", ", createEventDto.SelectedQuests.Select(quest => quest.Name));

        string categoryTitleText = createEventDto.Category?.Title ?? "None";

        string createdByText =
            createEventDto.Admin != null
                ? $"{createEventDto.Admin.Name} (ID: {createEventDto.Admin.UserId})"
                : "Unknown";

        string maximumPeopleText =
            createEventDto.MaximumPeople.HasValue
                ? createEventDto.MaximumPeople.Value.ToString()
                : "No limit";

        string bannerPathText = createEventDto.EventBannerPath ?? "None";

        return
            "Event created successfully!\n\n" +
            $"Name: {createEventDto.Name}\n" +
            $"Location: {createEventDto.Location}\n" +
            $"Start: {createEventDto.StartDateTime}\n" +
            $"End: {createEventDto.EndDateTime}\n" +
            $"Public: {createEventDto.IsPublic}\n" +
            $"Description: {createEventDto.Description}\n" +
            $"Maximum People: {maximumPeopleText}\n" +
            $"Banner Path: {bannerPathText}\n" +
            $"Category: {categoryTitleText}\n" +
            $"Selected Quests: {selectedQuestNamesText}\n" +
            $"Created By: {createdByText}";
    }

    private bool CanGoToStep2() =>
        !string.IsNullOrWhiteSpace(EventName) &&
        !string.IsNullOrWhiteSpace(Location);

    [RelayCommand]
    private void GoToStep3()
    {
        this.CurrentStep = 3;
    }

    [RelayCommand]
    private void GoBackToStep1()
    {
        this.CurrentStep = 1;
    }

    [RelayCommand]
    private void GoBackToStep2()
    {
        this.CurrentStep = 2;
    }

    [RelayCommand]
    private void Cancel()
    {
        this.EventCreationDetailsText = this.BuildEventCreationDetailsText(null);
        this.CloseRequested?.Invoke(null);
    }

    [RelayCommand(CanExecute = nameof(CanAddCustomQuest))]
    private void AddCustomQuest()
    {
        var quest = new Quest
        {
            Name = this.CustomQuestName.Trim(),
            Description = this.CustomQuestDescription.Trim(),
            Difficulty = 3,
        };

        this.SelectedQuests.Add(quest);
        this.CustomQuestName = string.Empty;
        this.CustomQuestDescription = string.Empty;
    }

    private bool CanAddCustomQuest() =>
        !string.IsNullOrWhiteSpace(this.CustomQuestName) &&
        !string.IsNullOrWhiteSpace(this.CustomQuestDescription);


    [RelayCommand]
    private void ToggleQuestSelection(Quest quest)
    {
        if (this.SelectedQuests.Contains(quest))
        {
            this.SelectedQuests.Remove(quest);
        }
        else
        {
            this.SelectedQuests.Add(quest);
        }
    }

    [RelayCommand]
    private void RemoveQuest(Quest quest)
    {
        if (this.SelectedQuests.Contains(quest))
        {
            this.SelectedQuests.Remove(quest);
        }
    }


    [RelayCommand]
    private async System.Threading.Tasks.Task CreateEvent()
    {
        var dto = this.BuildDto();
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
        int newEventId = await this.eventService.CreateEventAsync(eventEntity);
        eventEntity.EventId = newEventId;
        await this.attendedEventService.AttendEventAsync(newEventId, this.userService.GetCurrentUser().UserId);

        foreach (var quest in dto.SelectedQuests)
        {
            await this.questService.AddQuestAsync(eventEntity, quest);
        }

        this.EventCreationDetailsText = this.BuildEventCreationDetailsText(dto);
        this.CloseRequested?.Invoke(dto);
    }

    public CreateEventDto BuildDto()
    {
        int? maxPeople = int.TryParse(this.MaximumPeopleText, out var parsed) ? parsed : null;

        return new CreateEventDto
        {
            Name = this.EventName.Trim(),
            Location = this.Location.Trim(),
            StartDateTime = this.StartDate.Date + this.StartTime,
            EndDateTime = this.EndDate.Date + this.EndTime,
            IsPublic = this.IsPublic,
            Description = string.IsNullOrWhiteSpace(this.Description) ? "No description yet" : this.Description.Trim(),
            MaximumPeople = maxPeople,
            EventBannerPath = this.EventBannerPath,
            Category = this.SelectedCategory,
            Admin = this.userService.GetCurrentUser(),
            SelectedQuests = new List<Quest>(SelectedQuests),
        };
    }

    public async System.Threading.Tasks.Task LoadPresetQuestsAsync(IQuestService questService)
    {
        var quests = await questService.GetPresetQuestsAsync();
        this.AvailableQuests.Clear();
        foreach (var quest in quests)
        {
            this.AvailableQuests.Add(quest);
        }
    }

    public bool IsQuestSelected(Quest quest)
    {
        return this.SelectedQuests.Contains(quest);
    }
}

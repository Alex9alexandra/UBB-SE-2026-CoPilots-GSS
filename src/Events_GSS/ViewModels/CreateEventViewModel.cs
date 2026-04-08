using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Data.Services.ViewModelCore;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.ViewModels;

/// <summary>
/// ViewModel for creating a new event.
/// </summary>
public partial class CreateEventViewModel : ObservableObject
{
    private readonly CreateEventViewModelCore viewModelCore;

    // prevents Core->VM sync from re-triggering VM->Core setters (stack overflow)
    private bool isSyncingFromCore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventViewModel"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    /// <param name="eventService">The event service.</param>
    /// <param name="questService">The quest service.</param>
    /// <param name="attendedEventService">The attended event service.</param>
    public CreateEventViewModel(
        IUserService userService,
        IEventService eventService,
        IQuestService questService,
        IAttendedEventService attendedEventService)
    {
        this.viewModelCore = new CreateEventViewModelCore(userService, eventService, questService, attendedEventService);

        // keep listening for non-create actions (like step changes, quest loads, etc.)
        this.viewModelCore.StateChanged += this.OnViewModelCoreStateChanged;

        this.SyncCollectionsFromCore();
    }

    /// <summary>
    /// Gets or sets the current step in the event creation wizard.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep2Visible))]
    [NotifyPropertyChangedFor(nameof(IsStep3Visible))]
    private int currentStep = 1;

    /// <summary>
    /// Gets or sets the event creation details text for display.
    /// </summary>
    [ObservableProperty]
    private string eventCreationDetailsText = string.Empty;

    /// <summary>
    /// Gets a value indicating whether step 1 is visible.
    /// </summary>
    public Visibility IsStep1Visible => this.CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets a value indicating whether step 2 is visible.
    /// </summary>
    public Visibility IsStep2Visible => this.CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets a value indicating whether step 3 is visible.
    /// </summary>
    public Visibility IsStep3Visible => this.CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private string eventName = string.Empty;

    /// <summary>
    /// Gets or sets the event location.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private string location = string.Empty;

    /// <summary>
    /// Gets or sets the event start date.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private DateTimeOffset startDate = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the event start time.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private TimeSpan startTime = DateTime.Now.TimeOfDay;

    /// <summary>
    /// Gets or sets the event end date.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private DateTimeOffset endDate = DateTimeOffset.Now.AddDays(1);

    /// <summary>
    /// Gets or sets the event end time.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToStep2Command))]
    private TimeSpan endTime = DateTime.Now.AddHours(1).TimeOfDay;

    /// <summary>
    /// Gets or sets a value indicating whether the event is public.
    /// </summary>
    [ObservableProperty]
    private bool isPublic = true;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    private string? errorMessage;

    /// <summary>
    /// Gets a value indicating whether there is an error.
    /// </summary>
    public bool HasError => this.ErrorMessage is not null;

    /// <summary>
    /// Gets the visibility of the error message.
    /// </summary>
    public Visibility ErrorVisibility => this.HasError ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the event description.
    /// </summary>
    [ObservableProperty]
    private string description = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of people as text.
    /// </summary>
    [ObservableProperty]
    private string maximumPeopleText = string.Empty;

    /// <summary>
    /// Gets or sets the event banner image path.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBannerImage))]
    [NotifyPropertyChangedFor(nameof(BannerImageVisibility))]
    private string? eventBannerPath;

    /// <summary>
    /// Gets a value indicating whether a banner image is set.
    /// </summary>
    public bool HasBannerImage => !string.IsNullOrEmpty(this.EventBannerPath);

    /// <summary>
    /// Gets the visibility of the banner image.
    /// </summary>
    public Visibility BannerImageVisibility => this.HasBannerImage ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the selected category.
    /// </summary>
    [ObservableProperty]
    private Category? selectedCategory;

    /// <summary>
    /// Gets the available categories for the event.
    /// </summary>
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

    /// <summary>
    /// Gets the available quests that can be added to the event.
    /// </summary>
    public ObservableCollection<Quest> AvailableQuests { get; } = new();

    /// <summary>
    /// Gets the quests that have been selected for the event.
    /// </summary>
    public ObservableCollection<Quest> SelectedQuests { get; } = new();

    /// <summary>
    /// Gets or sets the custom quest name.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    private string customQuestName = string.Empty;

    /// <summary>
    /// Gets or sets the custom quest description.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomQuestCommand))]
    private string customQuestDescription = string.Empty;

    /// <summary>
    /// Event raised when the view should be closed.
    /// </summary>
    public event Action<CreateEventDto?>? CloseRequested;

    // === push VM -> _viewModelCore ===
    partial void OnCurrentStepChanged(int value) => _ = value; // driven by _viewModelCore

    partial void OnEventNameChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetEventName(value);
    }

    partial void OnLocationChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetLocation(value);
    }

    partial void OnStartDateChanged(DateTimeOffset value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetStartDate(value);
    }

    partial void OnStartTimeChanged(TimeSpan value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetStartTime(value);
    }

    partial void OnEndDateChanged(DateTimeOffset value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetEndDate(value);
    }

    partial void OnEndTimeChanged(TimeSpan value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetEndTime(value);
    }

    partial void OnIsPublicChanged(bool value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetIsPublic(value);
    }

    partial void OnDescriptionChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetDescription(value);
    }

    partial void OnMaximumPeopleTextChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetMaximumPeopleText(value);
    }

    partial void OnEventBannerPathChanged(string? value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetEventBannerPath(value);
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetSelectedCategory(value);
    }

    partial void OnCustomQuestNameChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetCustomQuestName(value);
    }

    partial void OnCustomQuestDescriptionChanged(string value)
    {
        if (this.isSyncingFromCore) return;
        this.viewModelCore.SetCustomQuestDescription(value);
    }

    // Commands
    [RelayCommand(CanExecute = nameof(CanGoToStep2))]
    private void GoToStep2() => this.viewModelCore.GoToStep2();

    private bool CanGoToStep2() => this.viewModelCore.CanGoToStep2;

    [RelayCommand]
    private void GoToStep3() => this.viewModelCore.GoToStep3();

    [RelayCommand]
    private void GoBackToStep1() => this.viewModelCore.GoBackToStep1();

    [RelayCommand]
    private void GoBackToStep2() => this.viewModelCore.GoBackToStep2();

    [RelayCommand]
    private void Cancel()
    {
        this.EventCreationDetailsText = this.viewModelCore.BuildEventCreationDetailsText(null);
        this.CloseRequested?.Invoke(null);
    }

    [RelayCommand(CanExecute = nameof(CanAddCustomQuest))]
    private void AddCustomQuest() => this.viewModelCore.AddCustomQuest();

    private bool CanAddCustomQuest() => this.viewModelCore.CanAddCustomQuest;

    [RelayCommand]
    private void ToggleQuestSelection(Quest quest) => this.viewModelCore.ToggleQuestSelection(quest);

    [RelayCommand]
    private void RemoveQuest(Quest quest) => this.viewModelCore.RemoveQuest(quest);

    // CHANGED: CreateEventViewModelCore.CreateEventAsync returns dto; VM sets text then closes
    [RelayCommand]
    private async Task CreateEvent()
    {
        CreateEventDto dto = await this.viewModelCore.CreateEventAsync();
        this.EventCreationDetailsText = this.viewModelCore.EventCreationDetailsText;
        this.CloseRequested?.Invoke(dto);
    }

    /// <summary>
    /// Builds the event DTO from current form data.
    /// </summary>
    /// <returns>A <see cref="CreateEventDto"/> containing the event data.</returns>
    public CreateEventDto BuildDto() => this.viewModelCore.BuildDto();

    /// <summary>
    /// Builds a text summary of the event creation details.
    /// </summary>
    /// <param name="createEventDto">The event DTO to build details from.</param>
    /// <returns>A string containing the event creation details.</returns>
    public string BuildEventCreationDetailsText(CreateEventDto? createEventDto) =>
        this.viewModelCore.BuildEventCreationDetailsText(createEventDto);

    /// <summary>
    /// Loads the preset quests asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadPresetQuestsAsync() =>
        await this.viewModelCore.LoadPresetQuestsAsync();

    /// <summary>
    /// Loads the preset quests asynchronously using the specified quest service.
    /// </summary>
    /// <param name="_">The quest service (unused, kept for backward compatibility).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadPresetQuestsAsync(IQuestService _) =>
        await this.viewModelCore.LoadPresetQuestsAsync();

    /// <summary>
    /// Determines whether the specified quest is selected.
    /// </summary>
    /// <param name="quest">The quest to check.</param>
    /// <returns><c>true</c> if the quest is selected; otherwise, <c>false</c>.</returns>
    public bool IsQuestSelected(Quest quest) => this.viewModelCore.IsQuestSelected(quest);

    private void OnViewModelCoreStateChanged()
    {
        if (this.isSyncingFromCore)
        {
            return;
        }

        this.isSyncingFromCore = true;
        try
        {
            // don't sync EventCreationDetailsText here; VM controls it for popup timing
            this.currentStep = this.viewModelCore.CurrentStep;
            this.errorMessage = this.viewModelCore.ErrorMessage;

            this.customQuestName = this.viewModelCore.CustomQuestName;
            this.customQuestDescription = this.viewModelCore.CustomQuestDescription;

            this.SyncCollectionsFromCore();

            this.OnPropertyChanged(string.Empty);

            this.GoToStep2Command.NotifyCanExecuteChanged();
            this.AddCustomQuestCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            this.isSyncingFromCore = false;
        }
    }

    private void SyncCollectionsFromCore()
    {
        this.AvailableQuests.Clear();
        foreach (var quest in this.viewModelCore.AvailableQuests)
        {
            this.AvailableQuests.Add(quest);
        }

        this.SelectedQuests.Clear();
        foreach (var quest in this.viewModelCore.SelectedQuests)
        {
            this.SelectedQuests.Add(quest);
        }
    }
}
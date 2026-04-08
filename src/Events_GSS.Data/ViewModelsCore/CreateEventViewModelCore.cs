using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

namespace Events_GSS.Data.Services.ViewModelCore
{
    /// <summary>
    /// Testable core logic extracted from CreateEventViewModel.
    /// No UI framework types, no CommunityToolkit MVVM attributes.
    /// </summary>
    public sealed class CreateEventViewModelCore
    {
        private readonly IUserService userService;
        private readonly IEventService eventService;
        private readonly IQuestService questService;
        private readonly IAttendedEventService attendedEventService;

        private readonly List<Quest> availableQuests = new ();
        private readonly List<Quest> selectedQuests = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEventViewModelCore"/> class.
        /// </summary>
        /// <param name="userService">The user service.</param>
        /// <param name="eventService">The event service.</param>
        /// <param name="questService">The quest service.</param>
        /// <param name="attendedEventService">The attended event service.</param>
        public CreateEventViewModelCore(
            IUserService userService,
            IEventService eventService,
            IQuestService questService,
            IAttendedEventService attendedEventService)
        {
            this.userService = userService;
            this.eventService = eventService;
            this.questService = questService;
            this.attendedEventService = attendedEventService;
        }

        /// <summary>
        /// Occurs when the state has changed.
        /// </summary>
        public event Action? StateChanged;

        /// <summary>
        /// Occurs when a close is requested.
        /// </summary>
        public event Action<CreateEventDto?>? CloseRequested;

        /// <summary>
        /// Gets the current step in the event creation process.
        /// </summary>
        public int CurrentStep { get; private set; } = 1;

        /// <summary>
        /// Gets the event creation details text.
        /// </summary>
        public string EventCreationDetailsText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the event name.
        /// </summary>
        public string EventName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the event location.
        /// </summary>
        public string Location { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the event start date.
        /// </summary>
        public DateTimeOffset StartDate { get; private set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets the event start time.
        /// </summary>
        public TimeSpan StartTime { get; private set; } = DateTime.Now.TimeOfDay;

        /// <summary>
        /// Gets the event end date.
        /// </summary>
        public DateTimeOffset EndDate { get; private set; } = DateTimeOffset.Now.AddDays(1);

        /// <summary>
        /// Gets the event end time.
        /// </summary>
        public TimeSpan EndTime { get; private set; } = DateTime.Now.AddHours(1).TimeOfDay;

        /// <summary>
        /// Gets a value indicating whether the event is public.
        /// </summary>
        public bool IsPublic { get; private set; } = true;

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the event description.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the maximum people text.
        /// </summary>
        public string MaximumPeopleText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the event banner path.
        /// </summary>
        public string? EventBannerPath { get; private set; }

        /// <summary>
        /// Gets the selected category.
        /// </summary>
        public Category? SelectedCategory { get; private set; }

        /// <summary>
        /// Gets the available quests.
        /// </summary>
        public IReadOnlyList<Quest> AvailableQuests => this.availableQuests;

        /// <summary>
        /// Gets the selected quests.
        /// </summary>
        public IReadOnlyList<Quest> SelectedQuests => this.selectedQuests;

        /// <summary>
        /// Gets the custom quest name.
        /// </summary>
        public string CustomQuestName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the custom quest description.
        /// </summary>
        public string CustomQuestDescription { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the user can go to step 2.
        /// </summary>
        public bool CanGoToStep2 =>
            !string.IsNullOrWhiteSpace(this.EventName) &&
            !string.IsNullOrWhiteSpace(this.Location);

        /// <summary>
        /// Gets a value indicating whether a custom quest can be added.
        /// </summary>
        public bool CanAddCustomQuest =>
            !string.IsNullOrWhiteSpace(this.CustomQuestName) &&
            !string.IsNullOrWhiteSpace(this.CustomQuestDescription);

        /// <summary>
        /// Sets the event name.
        /// </summary>
        /// <param name="value">The event name.</param>
        public void SetEventName(string value)
        {
            this.EventName = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the location.
        /// </summary>
        /// <param name="value">The location.</param>
        public void SetLocation(string value)
        {
            this.Location = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the start date.
        /// </summary>
        /// <param name="value">The start date.</param>
        public void SetStartDate(DateTimeOffset value)
        {
            this.StartDate = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the start time.
        /// </summary>
        /// <param name="value">The start time.</param>
        public void SetStartTime(TimeSpan value)
        {
            this.StartTime = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the end date.
        /// </summary>
        /// <param name="value">The end date.</param>
        public void SetEndDate(DateTimeOffset value)
        {
            this.EndDate = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the end time.
        /// </summary>
        /// <param name="value">The end time.</param>
        public void SetEndTime(TimeSpan value)
        {
            this.EndTime = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets a value indicating whether the event is public.
        /// </summary>
        /// <param name="value">True if the event is public; otherwise, false.</param>
        public void SetIsPublic(bool value)
        {
            this.IsPublic = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the error message.
        /// </summary>
        /// <param name="value">The error message.</param>
        public void SetErrorMessage(string? value)
        {
            this.ErrorMessage = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the description.
        /// </summary>
        /// <param name="value">The description.</param>
        public void SetDescription(string value)
        {
            this.Description = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the maximum people text.
        /// </summary>
        /// <param name="value">The maximum people text.</param>
        public void SetMaximumPeopleText(string value)
        {
            this.MaximumPeopleText = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the event banner path.
        /// </summary>
        /// <param name="value">The event banner path.</param>
        public void SetEventBannerPath(string? value)
        {
            this.EventBannerPath = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the selected category.
        /// </summary>
        /// <param name="value">The selected category.</param>
        public void SetSelectedCategory(Category? value)
        {
            this.SelectedCategory = value;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the custom quest name.
        /// </summary>
        /// <param name="value">The custom quest name.</param>
        public void SetCustomQuestName(string value)
        {
            this.CustomQuestName = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets the custom quest description.
        /// </summary>
        /// <param name="value">The custom quest description.</param>
        public void SetCustomQuestDescription(string value)
        {
            this.CustomQuestDescription = value ?? string.Empty;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Navigates to step 2 of the event creation process.
        /// </summary>
        public void GoToStep2()
        {
            this.ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(this.EventName))
            {
                this.ErrorMessage = "Event name is required.";
                this.StateChanged?.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Location))
            {
                this.ErrorMessage = "Location is required.";
                this.StateChanged?.Invoke();
                return;
            }

            var start = this.StartDate.Date + this.StartTime;
            var end = this.EndDate.Date + this.EndTime;

            if (end <= start)
            {
                this.ErrorMessage = "End date/time must be after start date/time.";
                this.StateChanged?.Invoke();
                return;
            }

            this.CurrentStep = 2;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Navigates to step 3 of the event creation process.
        /// </summary>
        public void GoToStep3()
        {
            this.CurrentStep = 3;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Navigates back to step 1 of the event creation process.
        /// </summary>
        public void GoBackToStep1()
        {
            this.CurrentStep = 1;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Navigates back to step 2 of the event creation process.
        /// </summary>
        public void GoBackToStep2()
        {
            this.CurrentStep = 2;
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Cancels the event creation process.
        /// </summary>
        public void Cancel()
        {
            this.EventCreationDetailsText = this.BuildEventCreationDetailsText(null);
            this.CloseRequested?.Invoke(null);
            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Adds a custom quest to the selected quests.
        /// </summary>
        public void AddCustomQuest()
        {
            if (!this.CanAddCustomQuest)
            {
                return;
            }

            var quest = new Quest
            {
                Name = this.CustomQuestName.Trim(),
                Description = this.CustomQuestDescription.Trim(),
                Difficulty = 3,
            };

            this.selectedQuests.Add(quest);
            this.CustomQuestName = string.Empty;
            this.CustomQuestDescription = string.Empty;

            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Toggles the selection state of a quest.
        /// </summary>
        /// <param name="quest">The quest to toggle.</param>
        public void ToggleQuestSelection(Quest quest)
        {
            if (this.selectedQuests.Contains(quest))
            {
                this.selectedQuests.Remove(quest);
            }
            else
            {
                this.selectedQuests.Add(quest);
            }

            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Removes a quest from the selected quests.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        public void RemoveQuest(Quest quest)
        {
            if (this.selectedQuests.Contains(quest))
            {
                this.selectedQuests.Remove(quest);
                this.StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Determines whether a quest is selected.
        /// </summary>
        /// <param name="quest">The quest to check.</param>
        /// <returns>True if the quest is selected; otherwise, false.</returns>
        public bool IsQuestSelected(Quest quest) => this.selectedQuests.Contains(quest);

        /// <summary>
        /// Builds the event creation DTO.
        /// </summary>
        /// <returns>The event creation DTO.</returns>
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
                SelectedQuests = new List<Quest>(this.selectedQuests),
            };
        }

        /// <summary>
        /// Builds the event creation details text.
        /// </summary>
        /// <param name="createEventDto">The event creation DTO.</param>
        /// <returns>The event creation details text.</returns>
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

        /// <summary>
        /// Loads the preset quests asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadPresetQuestsAsync()
        {
            var quests = await this.questService.GetPresetQuestsAsync();

            this.availableQuests.Clear();
            this.availableQuests.AddRange(quests);

            this.StateChanged?.Invoke();
        }

        /// <summary>
        /// Creates the event asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with the event creation DTO.</returns>
        public async Task<CreateEventDto> CreateEventAsync()
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

            this.StateChanged?.Invoke();

            return dto;
        }
    }
}
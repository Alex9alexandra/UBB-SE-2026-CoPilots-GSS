using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Services.ViewModelCore;
using Events_GSS.Data.Services.eventServices;
using Events_GSS.Data.Services.Interfaces;
using Events_GSS.Services.Interfaces;

using Moq;

using Xunit;

namespace Events_GSS.Tests.ViewModels
{
    public sealed class CreateEventViewModelCoreTests
    {
        private const int CurrentUserId = 1;

        private const int NewEventId = 555;

        private readonly Mock<IUserService> userServiceMock;
        private readonly Mock<IEventService> eventServiceMock;
        private readonly Mock<IQuestService> questServiceMock;
        private readonly Mock<IAttendedEventService> attendedEventServiceMock;

        private readonly CreateEventViewModelCore _viewModelCore;

        public CreateEventViewModelCoreTests()
        {
            // Setup
            this.userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
            this.eventServiceMock = new Mock<IEventService>(MockBehavior.Strict);
            this.questServiceMock = new Mock<IQuestService>(MockBehavior.Strict);
            this.attendedEventServiceMock = new Mock<IAttendedEventService>(MockBehavior.Strict);

            this._viewModelCore = MakeCore(
                this.userServiceMock,
                this.eventServiceMock,
                this.questServiceMock,
                this.attendedEventServiceMock);
        }

        [Fact]
        public void SetEventName_WhenNull_SetsEmptyStringAndRaisesStateChanged()
        {
            // Arrange
            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.SetEventName(null!);

            // Assert
            Assert.Equal(string.Empty, this._viewModelCore.EventName);
            Assert.True(stateChangedCount >= 1);

            this.userServiceMock.VerifyNoOtherCalls();
            this.eventServiceMock.VerifyNoOtherCalls();
            this.questServiceMock.VerifyNoOtherCalls();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void SetLocation_WhenNull_SetsEmptyStringAndRaisesStateChanged()
        {
            // Arrange
            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.SetLocation(null!);

            // Assert
            Assert.Equal(string.Empty, this._viewModelCore.Location);
            Assert.True(stateChangedCount >= 1);
        }

        [Fact]
        public void CanGoToStep2_WhenNameAndLocationNotWhitespace_IsTrue()
        {
            // Arrange
            this._viewModelCore.SetEventName("A");
            this._viewModelCore.SetLocation("B");

            // Act
            bool can = this._viewModelCore.CanGoToStep2;

            // Assert
            Assert.True(can);
        }

        [Fact]
        public void CanGoToStep2_WhenNameOrLocationWhitespace_IsFalse()
        {
            // Arrange
            this._viewModelCore.SetEventName("  ");
            this._viewModelCore.SetLocation("X");

            // Act
            bool can1 = this._viewModelCore.CanGoToStep2;

            // Assert
            Assert.False(can1);

            // Arrange
            this._viewModelCore.SetEventName("X");
            this._viewModelCore.SetLocation(" ");

            // Act
            bool can2 = this._viewModelCore.CanGoToStep2;

            // Assert
            Assert.False(can2);
        }

        [Fact]
        public void GoToStep2_WhenEventNameMissing_SetsErrorMessageAndDoesNotAdvanceStep()
        {
            // Arrange
            this._viewModelCore.SetEventName(" ");
            this._viewModelCore.SetLocation("Somewhere");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal(1, this._viewModelCore.CurrentStep);
            Assert.Equal("Event name is required.", this._viewModelCore.ErrorMessage);
            Assert.True(stateChangedCount >= 1);
        }

        [Fact]
        public void GoToStep2_WhenLocationMissing_SetsErrorMessageAndDoesNotAdvanceStep()
        {
            // Arrange
            this._viewModelCore.SetEventName("Party");
            this._viewModelCore.SetLocation("");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal(1, this._viewModelCore.CurrentStep);
            Assert.Equal("Location is required.", this._viewModelCore.ErrorMessage);
            Assert.True(stateChangedCount >= 1);
        }

        [Fact]
        public void GoToStep2_WhenEndIsNotAfterStart_SetsErrorMessageAndDoesNotAdvanceStep()
        {
            // Arrange
            this._viewModelCore.SetEventName("Party");
            this._viewModelCore.SetLocation("Here");

            this._viewModelCore.SetStartDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetStartTime(new TimeSpan(10, 0, 0));

            this._viewModelCore.SetEndDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetEndTime(new TimeSpan(10, 0, 0));

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal(1, this._viewModelCore.CurrentStep);
            Assert.Equal("End date/time must be after start date/time.", this._viewModelCore.ErrorMessage);
        }

        [Fact]
        public void GoToStep2_WhenValid_ClearsErrorAndAdvancesToStep2()
        {
            // Arrange
            this._viewModelCore.SetEventName("Party");
            this._viewModelCore.SetLocation("Here");

            this._viewModelCore.SetStartDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetStartTime(new TimeSpan(10, 0, 0));

            this._viewModelCore.SetEndDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetEndTime(new TimeSpan(11, 0, 0));

            this._viewModelCore.SetErrorMessage("some old error");

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal(2, this._viewModelCore.CurrentStep);
            Assert.Null(this._viewModelCore.ErrorMessage);
        }

        [Fact]
        public void GoToStep3_WhenCalled_AdvancesToStep3()
        {
            // Arrange
            this._viewModelCore.GoToStep2();

            // Act
            this._viewModelCore.GoToStep3();

            // Assert
            Assert.Equal(3, this._viewModelCore.CurrentStep);
        }

        [Fact]
        public void GoBackToStep1_WhenCalled_SetsStep1()
        {
            // Arrange
            this._viewModelCore.GoToStep3();

            // Act
            this._viewModelCore.GoBackToStep1();

            // Assert
            Assert.Equal(1, this._viewModelCore.CurrentStep);
        }

        [Fact]
        public void GoBackToStep2_WhenCalled_SetsStep2()
        {
            // Arrange
            this._viewModelCore.GoToStep3();

            // Act
            this._viewModelCore.GoBackToStep2();

            // Assert
            Assert.Equal(2, this._viewModelCore.CurrentStep);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenNull_ReturnsCancelledText()
        {
            // Act
            string text = this._viewModelCore.BuildEventCreationDetailsText(null);

            // Assert
            Assert.Equal("Event creation cancelled.", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_UsesFallbacks()
        {
            // Arrange
            var dto = new CreateEventDto
            {
                Name = "N",
                Location = "L",
                StartDateTime = new DateTime(2026, 04, 08, 10, 0, 0),
                EndDateTime = new DateTime(2026, 04, 08, 11, 0, 0),
                IsPublic = true,
                Description = "D",
                MaximumPeople = null,
                EventBannerPath = null,
                Category = null,
                Admin = null,
                SelectedQuests = new List<Quest>(),
            };

            // Act
            string text = this._viewModelCore.BuildEventCreationDetailsText(dto);

            // Assert
            Assert.Contains("Event created successfully!", text);
            Assert.Contains("Category: None", text);
            Assert.Contains("Selected Quests: None", text);
            Assert.Contains("Created By: Unknown", text);
            Assert.Contains("Maximum People: No limit", text);
            Assert.Contains("Banner Path: None", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_UsesActualValues()
        {
            // Arrange
            var dto = new CreateEventDto
            {
                Name = "N",
                Location = "L",
                StartDateTime = new DateTime(2026, 04, 08, 10, 0, 0),
                EndDateTime = new DateTime(2026, 04, 08, 11, 0, 0),
                IsPublic = false,
                Description = "D",
                MaximumPeople = 123,
                EventBannerPath = "banner.png",
                Category = new Category { Title = "MUSIC" },
                Admin = new User { Name = "AdminName", UserId = CurrentUserId },
                SelectedQuests = new List<Quest>
                {
                    new Quest { Name = "Q1" },
                    new Quest { Name = "Q2" },
                },
            };

            // Act
            string text = this._viewModelCore.BuildEventCreationDetailsText(dto);

            // Assert
            Assert.Contains("Category: MUSIC", text);
            Assert.Contains("Selected Quests: Q1, Q2", text);
            Assert.Contains("Created By: AdminName (ID: 1)", text);
            Assert.Contains("Maximum People: 123", text);
            Assert.Contains("Banner Path: banner.png", text);
            Assert.Contains("Public: False", text);
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId, Name = "U" };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this._viewModelCore.SetEventName("  Name  ");
            this._viewModelCore.SetLocation("  Loc  ");
            this._viewModelCore.SetDescription("   ");
            this._viewModelCore.SetMaximumPeopleText("not an int");
            this._viewModelCore.SetIsPublic(false);

            this._viewModelCore.SetStartDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetStartTime(new TimeSpan(10, 0, 0));
            this._viewModelCore.SetEndDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetEndTime(new TimeSpan(11, 0, 0));

            // Act
            CreateEventDto dto = this._viewModelCore.BuildDto();

            // Assert
            Assert.Equal("Name", dto.Name);
            Assert.Equal("Loc", dto.Location);
            Assert.Equal("No description yet", dto.Description);
            Assert.Null(dto.MaximumPeople);
            Assert.False(dto.IsPublic);
            Assert.Same(user, dto.Admin);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleIsInt_SetsValue()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this._viewModelCore.SetMaximumPeopleText("42");

            // Act
            CreateEventDto dto = this._viewModelCore.BuildDto();

            // Assert
            Assert.Equal(42, dto.MaximumPeople);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void CanAddCustomQuest_WhenWhitespace_IsFalse()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName(" ");
            this._viewModelCore.SetCustomQuestDescription("X");

            // Act + Assert
            Assert.False(this._viewModelCore.CanAddCustomQuest);

            // Arrange
            this._viewModelCore.SetCustomQuestName("X");
            this._viewModelCore.SetCustomQuestDescription(" ");

            // Act + Assert
            Assert.False(this._viewModelCore.CanAddCustomQuest);
        }

        [Fact]
        public void AddCustomQuest_WhenCannotAdd_DoesNothing()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName(" ");
            this._viewModelCore.SetCustomQuestDescription(" ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
            Assert.Empty(this._viewModelCore.SelectedQuests);
        }

        [Fact]
        public void AddCustomQuest_WhenValid_AddsQuestAndClearsInputs()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName("  QuestName  ");
            this._viewModelCore.SetCustomQuestDescription("  QuestDesc  ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
            Assert.Single(this._viewModelCore.SelectedQuests);
            Assert.Equal("QuestName", this._viewModelCore.SelectedQuests[0].Name);
            Assert.Equal("QuestDesc", this._viewModelCore.SelectedQuests[0].Description);

            Assert.Equal(string.Empty, this._viewModelCore.CustomQuestName);
            Assert.Equal(string.Empty, this._viewModelCore.CustomQuestDescription);
        }

        [Fact]
        public void ToggleQuestSelection_WhenQuestNotSelected_Adds()
        {
            // Arrange
            var quest = new Quest { Name = "Q" };

            // Act
            this._viewModelCore.ToggleQuestSelection(quest);

            // Assert
            Assert.True(this._viewModelCore.IsQuestSelected(quest));
        }

        [Fact]
        public void ToggleQuestSelection_WhenQuestAlreadySelected_Removes()
        {
            // Arrange
            var quest = new Quest { Name = "Q" };
            this._viewModelCore.ToggleQuestSelection(quest);
            Assert.True(this._viewModelCore.IsQuestSelected(quest));

            // Act
            this._viewModelCore.ToggleQuestSelection(quest);

            // Assert
            Assert.False(this._viewModelCore.IsQuestSelected(quest));
        }

        [Fact]
        public void RemoveQuest_WhenQuestSelected_Removes()
        {
            // Arrange
            var quest = new Quest { Name = "Q" };
            this._viewModelCore.ToggleQuestSelection(quest);
            Assert.True(this._viewModelCore.IsQuestSelected(quest));

            // Act
            this._viewModelCore.RemoveQuest(quest);

            // Assert
            Assert.False(this._viewModelCore.IsQuestSelected(quest));
        }

        [Fact]
        public void RemoveQuest_WhenQuestNotSelected_DoesNothing()
        {
            // Arrange
            var quest = new Quest { Name = "Q" };

            // Act
            this._viewModelCore.RemoveQuest(quest);

            // Assert
            Assert.False(this._viewModelCore.IsQuestSelected(quest));
        }

        [Fact]
        public async Task LoadPresetQuestsAsync_WhenCalled_ReplacesAvailableQuestsAndRaisesStateChanged()
        {
            // Arrange
            var expected = new List<Quest>
            {
                new Quest { Name = "Q1" },
                new Quest { Name = "Q2" },
            };

            this.questServiceMock
                .Setup(service => service.GetPresetQuestsAsync())
                .ReturnsAsync(expected);

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            await this._viewModelCore.LoadPresetQuestsAsync();

            // Assert
            Assert.Equal(2, this._viewModelCore.AvailableQuests.Count);
            Assert.Equal("Q1", this._viewModelCore.AvailableQuests[0].Name);
            Assert.Equal("Q2", this._viewModelCore.AvailableQuests[1].Name);
            Assert.True(stateChangedCount >= 1);

            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public void Cancel_WhenCalled_SetsCancelledTextAndRaisesCloseRequested()
        {
            // Arrange
            CreateEventDto? closedDto = new CreateEventDto(); // just a non-null sentinel
            this._viewModelCore.CloseRequested += dto => closedDto = dto;

            // Act
            this._viewModelCore.Cancel();

            // Assert
            Assert.Equal("Event creation cancelled.", this._viewModelCore.EventCreationDetailsText);
            Assert.Null(closedDto);
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_CreatesEvent_Attends_AddsQuests_SetsDetailsText_AndReturnsDto()
        {
            // Arrange
            var user = new User { UserId = CurrentUserId, Name = "Admin" };

            this.userServiceMock
                .Setup(service => service.GetCurrentUser())
                .Returns(user);

            this._viewModelCore.SetEventName("  My Event ");
            this._viewModelCore.SetLocation("  My Location ");
            this._viewModelCore.SetDescription("Desc");
            this._viewModelCore.SetMaximumPeopleText("10");
            this._viewModelCore.SetIsPublic(true);
            this._viewModelCore.SetEventBannerPath("banner.png");
            this._viewModelCore.SetSelectedCategory(new Category { CategoryId = 3, Title = "MUSIC" });

            this._viewModelCore.SetStartDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetStartTime(new TimeSpan(10, 0, 0));
            this._viewModelCore.SetEndDate(new DateTimeOffset(2026, 04, 08, 0, 0, 0, TimeSpan.Zero));
            this._viewModelCore.SetEndTime(new TimeSpan(12, 0, 0));

            // add quests
            this._viewModelCore.SetCustomQuestName("Q1");
            this._viewModelCore.SetCustomQuestDescription("D1");
            this._viewModelCore.AddCustomQuest();

            this._viewModelCore.SetCustomQuestName("Q2");
            this._viewModelCore.SetCustomQuestDescription("D2");
            this._viewModelCore.AddCustomQuest();

            Event? createdEventArg = null;

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
                .Callback<Event>(e => createdEventArg = e)
                .ReturnsAsync(NewEventId);

            this.attendedEventServiceMock
                .Setup(service => service.AttendEventAsync(NewEventId, CurrentUserId))
                .Returns(Task.CompletedTask);

            // AddQuestAsync should be called twice, with the event entity and each quest.
            this.questServiceMock
                .Setup(service => service.AddQuestAsync(
                    It.Is<Event>(e => e.EventId == NewEventId),
                    It.Is<Quest>(q => q.Name == "Q1")))
                .ReturnsAsync(0);

            this.questServiceMock
                .Setup(service => service.AddQuestAsync(
                    It.Is<Event>(e => e.EventId == NewEventId),
                    It.Is<Quest>(q => q.Name == "Q2")))
                .ReturnsAsync(0);

            // Act
            CreateEventDto dto = await this._viewModelCore.CreateEventAsync();

            // Assert
            Assert.NotNull(createdEventArg);
            Assert.Equal("My Event", createdEventArg!.Name);
            Assert.Equal("My Location", createdEventArg.Location);
            Assert.Equal(NewEventId, createdEventArg.EventId);

            Assert.Equal("My Event", dto.Name);
            Assert.Equal("My Location", dto.Location);
            Assert.Equal(10, dto.MaximumPeople);
            Assert.Same(user, dto.Admin);
            Assert.Equal(2, dto.SelectedQuests.Count);

            Assert.Contains("Event created successfully!", this._viewModelCore.EventCreationDetailsText);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        private static CreateEventViewModelCore MakeCore(
            Mock<IUserService> userServiceMock,
            Mock<IEventService> eventServiceMock,
            Mock<IQuestService> questServiceMock,
            Mock<IAttendedEventService> attendedEventServiceMock)
        {
            return new CreateEventViewModelCore(
                userServiceMock.Object,
                eventServiceMock.Object,
                questServiceMock.Object,
                attendedEventServiceMock.Object);
        }
    }
}
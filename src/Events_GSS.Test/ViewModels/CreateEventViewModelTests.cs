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

            this.userServiceMock.VerifyNoOtherCalls();
            this.eventServiceMock.VerifyNoOtherCalls();
            this.questServiceMock.VerifyNoOtherCalls();
            this.attendedEventServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void SetEventName_WhenNull_RaisesStateChanged()
        {
            // Arrange
            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.SetEventName(null!);

            // Assert
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
        }

        [Fact]
        public void SetLocation_WhenNull_RaisesStateChanged()
        {
            // Arrange
            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.SetLocation(null!);

            // Assert
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
        public void CanGoToStep2_WhenNameWhitespace_IsFalse()
        {
            // Arrange
            this._viewModelCore.SetEventName("  ");
            this._viewModelCore.SetLocation("X");

            // Act
            bool can = this._viewModelCore.CanGoToStep2;

            // Assert
            Assert.False(can);
        }

        [Fact]
        public void CanGoToStep2_WhenLocationWhitespace_IsFalse()
        {
            // Arrange
            this._viewModelCore.SetEventName("X");
            this._viewModelCore.SetLocation(" ");

            // Act
            bool can = this._viewModelCore.CanGoToStep2;

            // Assert
            Assert.False(can);
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
        }

        [Fact]
        public void GoToStep2_WhenEventNameMissing_SetsErrorMessage()
        {
            // Arrange
            this._viewModelCore.SetEventName(" ");
            this._viewModelCore.SetLocation("Somewhere");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal("Event name is required.", this._viewModelCore.ErrorMessage);
        }

        [Fact]
        public void GoToStep2_WhenEventNameMissing_RaisesStateChanged()
        {
            // Arrange
            this._viewModelCore.SetEventName(" ");
            this._viewModelCore.SetLocation("Somewhere");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
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
        }

        [Fact]
        public void GoToStep2_WhenLocationMissing_SetsErrorMessage()
        {
            // Arrange
            this._viewModelCore.SetEventName("Party");
            this._viewModelCore.SetLocation("");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
            Assert.Equal("Location is required.", this._viewModelCore.ErrorMessage);
        }

        [Fact]
        public void GoToStep2_WhenLocationMissing_RaisesStateChanged()
        {
            // Arrange
            this._viewModelCore.SetEventName("Party");
            this._viewModelCore.SetLocation("");

            var stateChangedCount = 0;
            this._viewModelCore.StateChanged += () => stateChangedCount++;

            // Act
            this._viewModelCore.GoToStep2();

            // Assert
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
        }

        [Fact]
        public void GoToStep2_WhenEndIsNotAfterStart_SetsErrorMessage()
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
        }

        [Fact]
        public void GoToStep2_WhenValid_ClearsError()
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
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_CategoryUsesFallback()
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
            Assert.Contains("Category: None", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_SelectedQuestsUsesFallback()
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
            Assert.Contains("Selected Quests: None", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_AdminUsesFallback()
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
            Assert.Contains("Created By: Unknown", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_MaximumPeopleUsesFallback()
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
            Assert.Contains("Maximum People: No limit", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasNoQuestsNoCategoryNoAdminNoMaxPeopleNoBanner_BannerUsesFallback()
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
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_SelectedQuestsUsesActualValues()
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
            Assert.Contains("Selected Quests: Q1, Q2", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_AdminUsesActualValues()
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
            Assert.Contains("Created By: AdminName (ID: 1)", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_MaxPeopleUsesActualValues()
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
            Assert.Contains("Maximum People: 123", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_BannerUsesActualValues()
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
            Assert.Contains("Banner Path: banner.png", text);
        }

        [Fact]
        public void BuildEventCreationDetailsText_WhenDtoHasQuestsCategoryAdminMaxPeopleBanner_PublicUsesActualValues()
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

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault_LocationIsTrimmed()
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
            Assert.Equal("Loc", dto.Location);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault_UsesDefaultDescription()
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
            Assert.Equal("No description yet", dto.Description);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault_SetsMaximumPeopleNull()
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
            Assert.Null(dto.MaximumPeople);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault_SetsIsPublicFalse()
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
            Assert.False(dto.IsPublic);

            this.userServiceMock.VerifyAll();
        }

        [Fact]
        public void BuildDto_WhenMaximumPeopleNotInt_SetsNull_AndWhenDescriptionWhitespace_UsesDefault_SetsAdmin()
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
        }

        [Fact]
        public void CanAddCustomQuest_WhenDescriptionWhitespace_IsFalse()
        {
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
        }

        [Fact]
        public void AddCustomQuest_WhenValid_SetsQuestName()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName("  QuestName  ");
            this._viewModelCore.SetCustomQuestDescription("  QuestDesc  ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
            Assert.Equal("QuestName", this._viewModelCore.SelectedQuests[0].Name);
        }

        [Fact]
        public void AddCustomQuest_WhenValid_SetsQuestDescription()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName("  QuestName  ");
            this._viewModelCore.SetCustomQuestDescription("  QuestDesc  ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
            Assert.Equal("QuestDesc", this._viewModelCore.SelectedQuests[0].Description);
        }

        [Fact]
        public void AddCustomQuest_WhenValid_ClearsCustomQuestName()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName("  QuestName  ");
            this._viewModelCore.SetCustomQuestDescription("  QuestDesc  ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
            Assert.Equal(string.Empty, this._viewModelCore.CustomQuestName);
        }

        [Fact]
        public void AddCustomQuest_WhenValid_ClearsCustomQuestDescription()
        {
            // Arrange
            this._viewModelCore.SetCustomQuestName("  QuestName  ");
            this._viewModelCore.SetCustomQuestDescription("  QuestDesc  ");

            // Act
            this._viewModelCore.AddCustomQuest();

            // Assert
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

            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadPresetQuestsAsync_WhenCalled_SetsQuest1()
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

            // Act
            await this._viewModelCore.LoadPresetQuestsAsync();

            // Assert
            Assert.Equal("Q1", this._viewModelCore.AvailableQuests[0].Name);

            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadPresetQuestsAsync_WhenCalled_SetsQuest2()
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

            // Act
            await this._viewModelCore.LoadPresetQuestsAsync();

            // Assert
            Assert.Equal("Q2", this._viewModelCore.AvailableQuests[1].Name);

            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task LoadPresetQuestsAsync_WhenCalled_RaisesStateChanged()
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
        }

        [Fact]
        public void Cancel_WhenCalled_RaisesCloseRequestedWithNull()
        {
            // Arrange
            CreateEventDto? closedDto = new CreateEventDto(); // just a non-null sentinel
            this._viewModelCore.CloseRequested += dto => closedDto = dto;

            // Act
            this._viewModelCore.Cancel();

            // Assert
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

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_SetsCreatedEventName()
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
            _ = await this._viewModelCore.CreateEventAsync();

            // Assert
            Assert.Equal("My Event", createdEventArg!.Name);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_SetsCreatedEventLocation()
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
            _ = await this._viewModelCore.CreateEventAsync();

            // Assert
            Assert.Equal("My Location", createdEventArg!.Location);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_SetsCreatedEventId()
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
            _ = await this._viewModelCore.CreateEventAsync();

            // Assert
            Assert.Equal(NewEventId, createdEventArg!.EventId);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_ReturnsDtoWithName()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            Assert.Equal("My Event", dto.Name);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_ReturnsDtoWithLocation()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            Assert.Equal("My Location", dto.Location);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_ReturnsDtoWithMaximumPeople()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            Assert.Equal(10, dto.MaximumPeople);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_ReturnsDtoWithAdmin()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            Assert.Same(user, dto.Admin);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_ReturnsDtoWithSelectedQuestsCount()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            Assert.Equal(2, dto.SelectedQuests.Count);

            this.userServiceMock.VerifyAll();
            this.eventServiceMock.VerifyAll();
            this.attendedEventServiceMock.VerifyAll();
            this.questServiceMock.VerifyAll();
        }

        [Fact]
        public async Task CreateEventAsync_WhenCalled_SetsDetailsText()
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

            this.eventServiceMock
                .Setup(service => service.CreateEventAsync(It.IsAny<Event>()))
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
            _ = await this._viewModelCore.CreateEventAsync();

            // Assert
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
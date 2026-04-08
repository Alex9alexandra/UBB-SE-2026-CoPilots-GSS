// <copyright file="MemoryViewModelCoreTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Events_GSS.Data.Models;
    using Events_GSS.Data.Services.Interfaces;
    using Events_GSS.Data.ViewModels;

    using Moq;

    using NUnit.Framework;

    /// <summary>
    /// Unit tests for the MemoryViewModelCore.
    /// </summary>
    public class MemoryViewModelCoreTests
    {
        private Mock<IMemoryService> mockMemoryService;
        private MemoryViewModelCore viewModel;

        /// <summary>
        /// Sets up the mocked dependencies.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.mockMemoryService = new Mock<IMemoryService>();

            this.mockMemoryService
                .Setup(s => s.OrderByDateAsync(It.IsAny<Event>(), It.IsAny<User>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Memory>());

            this.mockMemoryService
                .Setup(s => s.FilterByMyMemoriesAsync(It.IsAny<Event>(), It.IsAny<User>()))
                .ReturnsAsync(new List<Memory>());

            this.viewModel = new MemoryViewModelCore(this.mockMemoryService.Object);
        }

        [Test]
        public async Task InitializeAsync_LoadsMemories_SetsMemoriesProperty()
        {
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };

            var fakeMemories = new List<Memory>
            {
                new Memory { MemoryId = 1, Author = currentUser, Event = currentEvent }
            };

            this.mockMemoryService
                .Setup(s => s.OrderByDateAsync(currentEvent, currentUser, false))
                .ReturnsAsync(fakeMemories);

            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            Assert.That(this.viewModel.Memories.Count, Is.EqualTo(1));
            Assert.That(this.viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task AddMemoryAsync_ServiceThrowsException_SetsErrorMessage()
        {
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            this.mockMemoryService
                .Setup(s => s.AddAsync(currentEvent, currentUser, "photo.jpg", "test"))
                .ThrowsAsync(new InvalidOperationException("Reputation too low"));

            await this.viewModel.AddMemoryAsync("photo.jpg", "test");

            Assert.That(this.viewModel.ErrorMessage, Is.EqualTo("Reputation too low"));
            Assert.That(this.viewModel.HasError, Is.True);
        }

        [Test]
        public async Task ShowOnlyMine_ToggledTrue_CallsFilterService()
        {
            var currentEvent = new Event { EventId = 1 };
            var currentUser = new User { UserId = 1 };
            await this.viewModel.InitializeAsync(currentEvent, currentUser);

            this.viewModel.ShowOnlyMine = true;

            await Task.Delay(50);
            this.mockMemoryService.Verify(s => s.FilterByMyMemoriesAsync(currentEvent, currentUser), Times.Once);
        }
    }
}
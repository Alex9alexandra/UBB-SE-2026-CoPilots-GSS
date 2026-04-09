// <copyright file="IEventRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.eventRepository;

using System;

using Events_GSS.Data.Models;

/// <summary>
/// Defines the contract for the event repository, which provides methods to manage and retrieve event data in the system.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Asynchronously retrieves a list of all public and active events from the data source.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<List<Event>> GetAllPublicActiveAsync();

    /// <summary>
    /// Asynchronously retrieves a list of events that are associated with a specific user, identified by their user ID. This method is useful for fetching events that a particular user is enrolled in or has created.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Event"/> object representing the event with the specified ID, or null if the event does not exist.</returns>
    Task<Event?> GetByIdAsync(int eventId);

    /// <summary>
    /// Asynchronously adds a new event to the data source. This method takes an <see cref="Event"/> object as a parameter, which contains the details of the event to be added. The method returns a task that represents the asynchronous operation, and the result of the task is an integer representing the unique identifier of the newly created event in the data source.
    /// </summary>
    /// <param name="eventEntity">The <see cref="Event"/> object representing the event to be added.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the newly created event.</returns>
    Task<int> AddAsync(Event eventEntity);

    /// <summary>
    /// Asynchronously updates an existing event in the data source. This method takes an <see cref="Event"/> object as a parameter, which contains the updated details of the event. The method returns a task that represents the asynchronous operation.
    /// </summary>
    /// <param name="eventEntity">The <see cref="Event"/> object representing the event to be updated.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Event eventEntity);

    /// <summary>
    /// Asynchronously deletes an event from the data source based on its unique identifier. This method returns a task that represents the asynchronous operation.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to be deleted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(int eventId);
}
// <copyright file="IEventRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.Data.Repositories.eventRepository;

using System;


/// <summary>
/// Repository interface for managing event data operations.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Retrieves all public and active events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of public active events.</returns>
    Task<List<Event>> GetAllPublicActiveAsync();

    /// <summary>
    /// Retrieves an event by its unique identifier.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the event if found; otherwise, null.</returns>
    Task<Event?> GetByIdAsync(int eventId);

    /// <summary>
    /// Adds a new event to the repository.
    /// </summary>
    /// <param name="eventEntity">The event entity to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the identifier of the newly added event.</returns>
    Task<int> AddAsync(Event eventEntity);

    /// <summary>
    /// Updates an existing event in the repository.
    /// </summary>
    /// <param name="eventEntity">The event entity with updated information.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Event eventEntity);

    /// <summary>
    /// Deletes an event from the repository.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(int eventId);
}
using System;
using System.Collections.Generic;
using System.Text;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.categoriesRepository;

/// <summary>
/// Defines the contract for category repository operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets all categories asynchronously.
    /// </summary>
    /// <returns>A list of all categories.</returns>
    Task<List<Category>> GetAllAsync();

    /// <summary>
    /// Gets a category by its identifier asynchronously.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>A category if found; otherwise, null.</returns>
    Task<Category?> GetByIdAsync(int categoryId);
}
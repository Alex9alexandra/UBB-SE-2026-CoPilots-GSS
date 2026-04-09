using System;
using System.Collections.Generic;
using System.Text;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.categoryServices;

/// <summary>
/// Defines the contract for category-related operations.
/// </summary>
public interface ICategoryServices
{
    /// <summary>
    /// Retrieves all categories asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all categories.</returns>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Retrieves a category by its identifier asynchronously.
    /// </summary>
    /// <param name="categoryId">The unique identifier of the category.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the category if found; otherwise, null.</returns>
    Task<Category?> GetCategoryByIdAsync(int categoryId);
}

using System;
using System.Collections.Generic;

using Events_GSS.Data.Models;
using Events_GSS.Data.Database;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.categoriesRepository;

/// <summary>
/// Repository for managing category data operations.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The SQL connection factory.</param>
    public CategoryRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets all categories asynchronously.
    /// </summary>
    /// <returns>A list of all categories.</returns>
    public async Task<List<Category>> GetAllAsync()
    {
        var categories = new List<Category>();
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var selectCategoriesCommand = new SqlCommand("SELECT CategoryId, Title FROM Categories", connection);
        using var dataReader = await selectCategoriesCommand.ExecuteReaderAsync();
        while (await dataReader.ReadAsync())
        {
            categories.Add(new Category
            {
                CategoryId = (int)dataReader["CategoryId"],
                Title = (string)dataReader["Title"],
            });
        }

        return categories;
    }

    /// <summary>
    /// Gets a category by its identifier asynchronously.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>The category if found; otherwise, null.</returns>
    public async Task<Category?> GetByIdAsync(int categoryId)
    {
        using var connection = this.connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var selectCategoryCommand = new SqlCommand("SELECT CategoryId, Title FROM Categories WHERE CategoryId = @CategoryId", connection);
        selectCategoryCommand.Parameters.AddWithValue("@CategoryId", categoryId);

        using var dataReader = await selectCategoryCommand.ExecuteReaderAsync();
        if (await dataReader.ReadAsync())
        {
            return new Category
            {
                CategoryId = (int)dataReader["CategoryId"],
                Title = (string)dataReader["Title"],
            };
        }

        return null;
    }
}
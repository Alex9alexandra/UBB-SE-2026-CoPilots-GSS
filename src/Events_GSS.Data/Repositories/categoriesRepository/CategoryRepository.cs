using System;
using System.Collections.Generic;

using Events_GSS.Data.Models;
using Events_GSS.Data.Database;

using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Repositories.categoriesRepository;

public class CategoryRepository : ICategoryRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CategoryRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var categories = new List<Category>();
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand("SELECT CategoryId, Title FROM Categories", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                CategoryId = (int)reader["CategoryId"],
                Title = (string)reader["Title"]
            });
        }

        return categories;
    }

    public async Task<Category?> GetByIdAsync(int categoryId)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new SqlCommand("SELECT CategoryId, Title FROM Categories WHERE CategoryId = @CategoryId", conn);
        cmd.Parameters.AddWithValue("@CategoryId", categoryId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Category
            {
                CategoryId = (int)reader["CategoryId"],
                Title = (string)reader["Title"]
            };
        }

        return null;
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using Events_GSS.Data.Models;

using Events_GSS.Data.Repositories.categoriesRepository;

namespace Events_GSS.Data.Services.categoryServices;

internal class CategoryServices: ICategoryServices
{
    private readonly ICategoryRepository _categoryRepository;
    public CategoryServices(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }
    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _categoryRepository.GetByIdAsync(categoryId);
    }
}

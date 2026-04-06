using System;
using System.Collections.Generic;
using System.Text;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Services.categoryServices;

public interface ICategoryServices
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int categoryId);
}

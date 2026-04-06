using System;
using System.Collections.Generic;
using System.Text;
using Events_GSS.Data.Models;

namespace Events_GSS.Data.Repositories.categoriesRepository;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int categoryId);
}

using System.Collections.Generic;
using System.Threading.Tasks;

using Events_GSS.Data.Models;
using Events_GSS.Data.Repositories.categoriesRepository;
using Events_GSS.Data.Services.categoryServices;

using Moq;

using Xunit;

namespace Events_GSS.Tests.Services
{
    public sealed class CategoryServicesTests
    {
        private const int ExampleCategoryId = 11;

        private readonly Mock<ICategoryRepository> categoryRepositoryMock;
        private readonly CategoryServices categoryServices;

        public CategoryServicesTests()
        {
            // Setup
            this.categoryRepositoryMock = new Mock<ICategoryRepository>(MockBehavior.Strict);
            this.categoryServices = MakeCategoryServices(this.categoryRepositoryMock);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedCategories = new List<Category>
            {
                new Category(),
                new Category(),
            };

            this.categoryRepositoryMock
                .Setup(repository => repository.GetAllAsync())
                .ReturnsAsync(expectedCategories);

            // Act
            List<Category> actualCategories = await this.categoryServices.GetAllCategoriesAsync();

            // Assert
            Assert.Same(expectedCategories, actualCategories);

            this.categoryRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WhenCalled_ReturnsRepositoryResult()
        {
            // Arrange
            var expectedCategory = new Category();

            this.categoryRepositoryMock
                .Setup(repository => repository.GetByIdAsync(ExampleCategoryId))
                .ReturnsAsync(expectedCategory);

            // Act
            Category? actualCategory = await this.categoryServices.GetCategoryByIdAsync(ExampleCategoryId);

            // Assert
            Assert.Same(expectedCategory, actualCategory);

            this.categoryRepositoryMock.VerifyAll();
        }

        private static CategoryServices MakeCategoryServices(Mock<ICategoryRepository> categoryRepositoryMock)
        {
            return new CategoryServices(categoryRepositoryMock.Object);
        }
    }
}
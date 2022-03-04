using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Components
{
    public class GenreMenuComponentTest : IClassFixture<SqliteInMemoryFixture>
    {
        private readonly SqliteInMemoryFixture _fixture;

        public GenreMenuComponentTest(SqliteInMemoryFixture fixture)
        {
            _fixture = fixture;
            _fixture.CreateDatabase();
        }

        [Fact]
        public async Task GenreMenuComponent_Returns_NineGenres()
        {
            // Arrange
            var dbContext = _fixture.Context;
            var genreMenuComponent = new GenreMenuComponent(dbContext);

            PopulateData(dbContext);

            // Act
            var result = await genreMenuComponent.InvokeAsync();

            // Assert
            Assert.NotNull(result);
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Null(viewResult.ViewName);
            var genreResult = Assert.IsType<List<string>>(viewResult.ViewData.Model);
            Assert.Equal(9, genreResult.Count);
        }

        private static void PopulateData(MusicStoreContext context)
        {
            var genres = Enumerable.Range(1, 10).Select(n => new Genre { GenreId = n, Name = $"G{n}" });

            context.AddRange(genres);
            context.SaveChanges();
        }
    }
}

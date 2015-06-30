using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Components
{
    public class GenreMenuComponentTest
    {
        private readonly IServiceProvider _serviceProvider;

        public GenreMenuComponentTest()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework()
                      .AddInMemoryDatabase()
                      .AddDbContext<MusicStoreContext>(options => options.UseInMemoryDatabase());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task GenreMenuComponent_Returns_NineGenres()
        {
            // Arrange
            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var genreMenuComponent = new GenreMenuComponent(dbContext);

            PopulateData(dbContext);

            // Act
            var result = await genreMenuComponent.InvokeAsync();

            // Assert
            Assert.NotNull(result);
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Null(viewResult.ViewName);
            var genreResult = Assert.IsType<List<Genre>>(viewResult.ViewData.Model);
            Assert.Equal(9, genreResult.Count);
        }

        private static void PopulateData(MusicStoreContext context)
        {
            var genres = Enumerable.Range(1, 10).Select(n => new Genre());

            context.AddRange(genres);
            context.SaveChanges();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Controllers
{
    public class StoreControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public StoreControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryDatabase()
                      .AddDbContext<MusicStoreContext>(options => options.UseInMemoryDatabase());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Index_CreatesViewWithGenres()
        {
            // Arrange
            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            CreateTestGenres(numberOfGenres: 10, numberOfAlbums: 1, dbContext: dbContext);

            var controller = new StoreController()
            {
                DbContext = dbContext,
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            var viewModel = Assert.IsType<List<Genre>>(viewResult.ViewData.Model);
            Assert.Equal(10, viewModel.Count);
        }

        [Fact]
        public async Task Browse_ReturnsHttpNotFoundWhenNoGenreData()
        {
            // Arrange
            var controller = new StoreController()
            {
                DbContext = _serviceProvider.GetRequiredService<MusicStoreContext>(),
            };

            // Act
            var result = await controller.Browse(string.Empty);

            // Assert
            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public async Task Browse_ReturnsViewWithGenre()
        {
            // Arrange
            var genreName = "Genre 1";

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            CreateTestGenres(numberOfGenres: 3, numberOfAlbums: 3, dbContext: dbContext);

            var controller = new StoreController()
            {
                DbContext = dbContext,
            };

            // Act
            var result = await controller.Browse(genreName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            var viewModel = Assert.IsType<Genre>(viewResult.ViewData.Model);
            Assert.Equal(genreName, viewModel.Name);
            Assert.NotNull(viewModel.Albums);
            Assert.Equal(3, viewModel.Albums.Count);
        }

        [Fact]
        public async Task Details_ReturnsHttpNotFoundWhenNoAlbumData()
        {
            // Arrange
            var albumId = int.MinValue;
            var controller = new StoreController()
            {
                DbContext = _serviceProvider.GetRequiredService<MusicStoreContext>(),
                Cache = _serviceProvider.GetRequiredService<IMemoryCache>(),
            };

            // Act
            var result = await controller.Details(albumId);

            // Assert
            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsAlbumDetail()
        {
            // Arrange
            var albumId = 1;

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var genres = CreateTestGenres(numberOfGenres: 3, numberOfAlbums: 3, dbContext: dbContext);

            var cache = _serviceProvider.GetRequiredService<IMemoryCache>();

            var controller = new StoreController()
            {
                DbContext = dbContext,
                Cache = cache,
            };

            // Act
            var result = await controller.Details(albumId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            var viewModel = Assert.IsType<Album>(viewResult.ViewData.Model);
            Assert.NotNull(viewModel.Genre);
            var genre = genres.SingleOrDefault(g => g.GenreId == viewModel.GenreId);
            Assert.NotNull(genre);
            Assert.NotNull(genre.Albums.SingleOrDefault(a => a.AlbumId == albumId));
            Assert.Null(viewModel.Artist);

            var cachedAlbum = cache.Get<Album>("album_1");
            Assert.NotNull(cachedAlbum);
            Assert.Equal(albumId, cachedAlbum.AlbumId);
        }

        private static Genre[] CreateTestGenres(int numberOfGenres, int numberOfAlbums, DbContext dbContext)
        {
            var albums = Enumerable.Range(1, numberOfAlbums * numberOfGenres).Select(n =>
                  new Album()
                  {
                      AlbumId = n,
                  }).ToList();

            var generes = Enumerable.Range(1, numberOfGenres).Select(n =>
                 new Genre()
                 {
                     Albums = albums.Where(i => i.AlbumId % numberOfGenres == n - 1).ToList(),
                     GenreId = n,
                     Name = "Genre " + n,
                 });

            dbContext.AddRange(albums);
            dbContext.AddRange(generes);
            dbContext.SaveChanges();

            return generes.ToArray();
        }
    }
}
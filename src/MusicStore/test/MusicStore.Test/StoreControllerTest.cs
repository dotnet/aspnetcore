using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;
using MusicStore.Test;
using Xunit;

namespace MusicStore.Controllers
{
    public class StoreControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public StoreControllerTest()
        {
            var efServiceProvider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

            var services = new ServiceCollection();

            services.AddDbContext<MusicStoreContext>(b => b.UseInMemoryDatabase("Scratch").UseInternalServiceProvider(efServiceProvider));

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Index_CreatesViewWithGenres()
        {
            // Arrange
            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            CreateTestGenres(numberOfGenres: 10, numberOfAlbums: 1, dbContext: dbContext);

            var controller = new StoreController(dbContext, new TestAppSettings());

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
            var controller = new StoreController(
                _serviceProvider.GetRequiredService<MusicStoreContext>(),
                new TestAppSettings());

            // Act
            var result = await controller.Browse(string.Empty);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Browse_ReturnsViewWithGenre()
        {
            // Arrange
            var genreName = "Genre 1";

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            CreateTestGenres(numberOfGenres: 3, numberOfAlbums: 3, dbContext: dbContext);

            var controller = new StoreController(dbContext, new TestAppSettings());

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
            var controller = new StoreController(
                _serviceProvider.GetRequiredService<MusicStoreContext>(),
                 new TestAppSettings());

            // Act
            var result = await controller.Details(_serviceProvider.GetRequiredService<IMemoryCache>(), albumId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsAlbumDetail()
        {
            // Arrange
            var albumId = 1;

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var genres = CreateTestGenres(numberOfGenres: 3, numberOfAlbums: 3, dbContext: dbContext);

            var cache = _serviceProvider.GetRequiredService<IMemoryCache>();

            var controller = new StoreController(dbContext, new TestAppSettings());

            // Act
            var result = await controller.Details(cache, albumId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            var viewModel = Assert.IsType<Album>(viewResult.ViewData.Model);
            Assert.NotNull(viewModel.Genre);
            var genre = genres.SingleOrDefault(g => g.GenreId == viewModel.GenreId);
            Assert.NotNull(genre);
            Assert.NotNull(genre.Albums.SingleOrDefault(a => a.AlbumId == albumId));
            Assert.NotNull(viewModel.Artist);
            Assert.Equal(1, viewModel.ArtistId);

            var cachedAlbum = cache.Get<Album>("album_1");
            Assert.NotNull(cachedAlbum);
            Assert.Equal(albumId, cachedAlbum.AlbumId);
        }

        private static Genre[] CreateTestGenres(int numberOfGenres, int numberOfAlbums, DbContext dbContext)
        {
            var artist = new Artist();
            artist.ArtistId = 1;
            artist.Name = "Artist1";

            var albums = Enumerable.Range(1, numberOfAlbums * numberOfGenres).Select(n =>
                  new Album()
                  {
                      AlbumId = n,
                      Artist = artist,
                      ArtistId = artist.ArtistId
                  }).ToList();

            var generes = Enumerable.Range(1, numberOfGenres).Select(n =>
                 new Genre()
                 {
                     Albums = albums.Where(i => i.AlbumId % numberOfGenres == n - 1).ToList(),
                     GenreId = n,
                     Name = "Genre " + n
                 });

            var artis = Enumerable.Range(1, numberOfGenres).Select(n =>
                 new Genre()
                 {
                     Albums = albums.Where(i => i.AlbumId % numberOfGenres == n - 1).ToList(),
                     GenreId = n,
                     Name = "Genre " + n,
                 });

            dbContext.Add(artist);
            dbContext.AddRange(albums);
            dbContext.AddRange(generes);
            dbContext.SaveChanges();

            return generes.ToArray();
        }
    }
}
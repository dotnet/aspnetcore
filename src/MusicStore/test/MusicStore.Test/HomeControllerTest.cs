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
    public class HomeControllerTest : IClassFixture<SqliteInMemoryFixture>
    {
        private readonly SqliteInMemoryFixture _fixture;

        public HomeControllerTest(SqliteInMemoryFixture fixture)
        {
            _fixture = fixture;
            _fixture.CreateDatabase();
        }

        [Fact]
        public void Error_ReturnsErrorView()
        {
            // Arrange
            var controller = new HomeController(new TestAppSettings());
            var errorView = "~/Views/Shared/Error.cshtml";

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(errorView, viewResult.ViewName);
        }

        [Fact]
        public async Task Index_GetsSixTopAlbums()
        {
            // Arrange
            var dbContext = _fixture.Context;
            var cache = _fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            var controller = new HomeController(new TestAppSettings());
            PopulateData(dbContext);

            // Action
            var result = await controller.Index(dbContext, cache);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            Assert.NotNull(viewResult.ViewData.Model);

            var albums = Assert.IsType<List<Album>>(viewResult.ViewData.Model);
            Assert.Equal(6, albums.Count);
        }

        [Fact]
        public void StatusCodePage_ReturnsStatusCodePage()
        {
            // Arrange
            var controller = new HomeController(new TestAppSettings());
            var statusCodeView = "~/Views/Shared/StatusCodePage.cshtml";

            // Action
            var result = controller.StatusCodePage();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(statusCodeView, viewResult.ViewName);
        }

        private void PopulateData(DbContext context)
        {
            var albums = TestAlbumDataProvider.GetAlbums();

            foreach (var album in albums)
            {
                context.Add(album);
            }

            context.SaveChanges();
        }

        private class TestAlbumDataProvider
        {
            public static Album[] GetAlbums()
            {
                var generes = Enumerable.Range(1, 10).Select(n =>
                    new Genre()
                    {
                        GenreId = n,
                        Name = "Genre Name " + n,
                    }).ToArray();

                var artists = Enumerable.Range(1, 10).Select(n =>
                    new Artist()
                    {
                        ArtistId = n + 1,
                        Name = "Artist Name " + n,
                    }).ToArray();

                var albums = Enumerable.Range(1, 10).Select(n =>
                    new Album
                    {
                        Artist = artists[n - 1],
                        ArtistId = n,
                        Genre = generes[n - 1],
                        GenreId = n,
                        Title = "Greatest Hits",
                    }).ToArray();

                return albums;
            }
        }
    }
}

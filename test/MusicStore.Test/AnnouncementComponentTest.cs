using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Components
{
    public class AnnouncementComponentTest
    {
        private readonly IServiceProvider _serviceProvider;

        public AnnouncementComponentTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryDatabase()
                      .AddDbContext<MusicStoreContext>(options =>
                            options.UseInMemoryDatabase());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task AnnouncementComponent_Returns_LatestAlbum()
        {
            // Arrange
            var today = new DateTime(year: 2002, month: 10, day: 30);

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
            var clock = new TestSystemClock() { UtcNow = today };

            var announcementComponent = new AnnouncementComponent(dbContext, cache, clock);

            PopulateData(dbContext, latestAlbumDate: today);

            // Action
            var result = await announcementComponent.InvokeAsync();

            // Assert
            Assert.NotNull(result);
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Null(viewResult.ViewName);
            var albumResult = Assert.IsType<Album>(viewResult.ViewData.Model);
            Assert.Equal(today, albumResult.Created.Date);
        }

        private static void PopulateData(DbContext context, DateTime latestAlbumDate)
        {
            var albums = TestAlbumDataProvider.GetAlbums(latestAlbumDate);

            foreach (var album in albums)
            {
                context.Add(album);
            }

            context.SaveChanges();
        }

        private class TestAlbumDataProvider
        {
            public static Album[] GetAlbums(DateTime latestAlbumDate)
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
                    new Album()
                    {
                        Artist = artists[n - 1],
                        ArtistId = n,
                        Genre = generes[n - 1],
                        GenreId = n,
                        Created = latestAlbumDate.AddDays(1 - n),
                    }).ToArray();

                return albums;
            }
        }

        private class TestSystemClock : ISystemClock
        {
            public DateTime UtcNow { get; set; }
        }
    }
}

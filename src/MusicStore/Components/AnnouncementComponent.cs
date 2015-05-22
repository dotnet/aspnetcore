using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.Caching.Memory;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "Announcement")]
    public class AnnouncementComponent : ViewComponent
    {
        public AnnouncementComponent(MusicStoreContext dbContext, IMemoryCache cache, ISystemClock clock)
        {
            DbContext = dbContext;
            Cache = cache;
            Clock = clock;
        }

        private MusicStoreContext DbContext { get; }

        private IMemoryCache Cache { get; }

        private ISystemClock Clock { get; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cacheKey = "latestAlbum";
            Album latestAlbum;
            if(!Cache.TryGetValue(cacheKey, out latestAlbum))
            {
                latestAlbum = await GetLatestAlbum();

                if (latestAlbum != null)
                {
                    Cache.Set(
                        cacheKey,
                        latestAlbum,
                        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
                }
            }

            return View(latestAlbum);
        }

        private async Task<Album> GetLatestAlbum()
        {
            var latestAlbum = await DbContext.Albums
                .OrderByDescending(a => a.Created)
                .Where(a => (a.Created - Clock.UtcNow).TotalDays <= 2)
                .FirstOrDefaultAsync();

            return latestAlbum;
        }
    }
}
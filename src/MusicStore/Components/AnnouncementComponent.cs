using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Cache.Memory;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "Announcement")]
    public class AnnouncementComponent : ViewComponent
    {
        private readonly MusicStoreContext _dbContext;
        private readonly IMemoryCache _cache;

        public AnnouncementComponent(MusicStoreContext dbContext, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _cache = memoryCache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var latestAlbum = await _cache.GetOrSet("latestAlbum", async context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                return await GetLatestAlbum();
            });

            return View(latestAlbum);
        }

        private Task<Album> GetLatestAlbum()
        {
            var latestAlbum = _dbContext.Albums
                .OrderByDescending(a => a.Created)
                .Where(a => (a.Created - DateTime.UtcNow).TotalDays <= 2)
                .FirstOrDefaultAsync();

            return latestAlbum;
        }
    }
}
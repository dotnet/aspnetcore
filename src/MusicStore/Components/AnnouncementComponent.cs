using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using Microsoft.Framework.Cache.Memory;

namespace MusicStore.Components
{
    [ViewComponent(Name = "Announcement")]
    public class AnnouncementComponent : ViewComponent
    {
        private readonly MusicStoreContext db;
        private readonly IMemoryCache cache;

        public AnnouncementComponent(MusicStoreContext context, IMemoryCache memoryCache)
        {
            db = context;
            cache = memoryCache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var latestAlbum = await cache.GetOrAdd("latestAlbum", async context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                return await GetLatestAlbum();
            });

            return View(latestAlbum);
        }

        private Task<Album> GetLatestAlbum()
        {
            var latestAlbum = db.Albums.OrderByDescending(a => a.Created).FirstOrDefault();
            if ((latestAlbum.Created - DateTime.UtcNow).TotalDays <= 2)
            {
                return Task.FromResult(latestAlbum);
            }
            else
            {
                return Task.FromResult<Album>(null);
            }
        }
    }
}
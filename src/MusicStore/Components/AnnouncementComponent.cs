using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Caching.Memory;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "Announcement")]
    public class AnnouncementComponent : ViewComponent
    {
        [Activate]
        public MusicStoreContext DbContext
        {
            get;
            set;
        }

        [Activate]
        public IMemoryCache Cache
        {
            get;
            set;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var latestAlbum = await Cache.GetOrSet("latestAlbum", async context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                return await GetLatestAlbum();
            });

            return View(latestAlbum);
        }

        private Task<Album> GetLatestAlbum()
        {
            var latestAlbum = DbContext.Albums
                .OrderByDescending(a => a.Created)
                .Where(a => (a.Created - DateTime.UtcNow).TotalDays <= 2)
                .FirstOrDefaultAsync();

            return latestAlbum;
        }
    }
}
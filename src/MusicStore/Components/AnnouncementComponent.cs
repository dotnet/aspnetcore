using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "Announcement")]
    public class AnnouncementComponent : ViewComponent
    {
        private readonly MusicStoreContext db;

        public AnnouncementComponent(MusicStoreContext context)
        {
            db = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var latestAlbum = await GetLatestAlbum();
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
using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Cache.Memory;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly MusicStoreContext db;
        private readonly IMemoryCache cache;

        public StoreController(MusicStoreContext context, IMemoryCache memoryCache)
        {
            db = context;
            cache = memoryCache;
        }

        //
        // GET: /Store/

        public IActionResult Index()
        {
            var genres = db.Genres.ToList();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco

        public IActionResult Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database
            var genreModel = db.Genres.Include(g => g.Albums).Where(g => g.Name == genre).FirstOrDefault();
            return View(genreModel);
        }

        public IActionResult Details(int id)
        {
            var album = cache.GetOrSet(string.Format("album_{0}", id), context =>
            {
                //Remove it from cache if not retrieved in last 10 minutes
                context.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                var albumData = db.Albums.Where(a => a.AlbumId == id).Include(a => a.Artist).Include(a => a.Genre).ToList().FirstOrDefault();
                return albumData;
            });

            return View(album);
        }
    }
}
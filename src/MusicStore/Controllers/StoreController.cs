using Microsoft.Framework.Cache.Memory;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System;
using System.Linq;

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

            // TODO [EF] Swap to native support for loading related data when available
            var genreModel = db.Genres.Single(g => g.Name == genre);
            genreModel.Albums = db.Albums.Where(a => a.GenreId == genreModel.GenreId).ToList();

            return View(genreModel);
        }

        public IActionResult Details(int id)
        {
            var album = cache.GetOrAdd(string.Format("album_{0}", id), context =>
            {
                //Remove it from cache if not retrieved in last 10 minutes
                context.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                var albumData = db.Albums.Single(a => a.AlbumId == id);

                // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
                albumData.Genre = db.Genres.Single(g => g.GenreId == albumData.GenreId);
                albumData.Artist = db.Artists.Single(a => a.ArtistId == albumData.ArtistId);
                return albumData;
            });

            return View(album);
        }
    }
}
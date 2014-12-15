using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Cache.Memory;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly MusicStoreContext _dbContext;
        private readonly IMemoryCache _cache;

        public StoreController(MusicStoreContext context, IMemoryCache memoryCache)
        {
            _dbContext = context;
            _cache = memoryCache;
        }

        //
        // GET: /Store/

        public async Task<IActionResult> Index()
        {
            var genres = await _dbContext.Genres.ToListAsync();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco

        public async Task<IActionResult> Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database
            var genreModel = await _dbContext.Genres
                .Include(g => g.Albums)
                .Where(g => g.Name == genre)
                .FirstOrDefaultAsync();

            return View(genreModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var album = await _cache.GetOrSet(string.Format("album_{0}", id), async context =>
            {
                //Remove it from cache if not retrieved in last 10 minutes
                context.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                return await _dbContext.Albums
                    .Where(a => a.AlbumId == id)
                    .Include(a => a.Artist)
                    .Include(a => a.Genre)
                    .FirstOrDefaultAsync();
            });

            return View(album);
        }
    }
}
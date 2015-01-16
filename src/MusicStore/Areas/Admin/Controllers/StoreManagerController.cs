using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Data.Entity;
using Microsoft.Framework.Cache.Memory;
using MusicStore.Hubs;
using MusicStore.Models;
using MusicStore.ViewModels;

namespace MusicStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNet.Mvc.Authorize("ManageStore")]
    public class StoreManagerController : Controller
    {
        private readonly MusicStoreContext _dbContext;
        private readonly IMemoryCache _cache;
        private IHubContext _announcementHub;

        public StoreManagerController(
            MusicStoreContext dbContext,
            IConnectionManager connectionManager,
            IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _announcementHub = connectionManager.GetHubContext<AnnouncementHub>();
            _cache = memoryCache;
        }

        //
        // GET: /StoreManager/

        public async Task<IActionResult> Index()
        {
            var albums = await _dbContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .ToListAsync();

            return View(albums);
        }

        //
        // GET: /StoreManager/Details/5

        public async Task<IActionResult> Details(int id)
        {
            var cacheKey = GetCacheKey(id);

            var album = await _cache.GetOrSet(cacheKey, async context =>
            {
                //Remove it from cache if not retrieved in last 10 minutes.
                context.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                //If this returns null how do we prevent the cache to store this.
                return await _dbContext.Albums
                    .Where(a => a.AlbumId == id)
                    .Include(a => a.Artist)
                    .Include(a => a.Genre)
                    .FirstOrDefaultAsync();
            });

            if (album == null)
            {
                _cache.Remove(cacheKey);
                return View(album);
            }

            return View(album);
        }

        //
        // GET: /StoreManager/Create
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(_dbContext.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(_dbContext.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Album album)
        {
            if (ModelState.IsValid)
            {
                await _dbContext.Albums.AddAsync(album, Context.RequestAborted);
                await _dbContext.SaveChangesAsync(Context.RequestAborted);

                var albumData = new AlbumData
                {
                    Title = album.Title,
                    Url = Url.Action("Details", "Store", new { id = album.AlbumId })
                };

                _announcementHub.Clients.All.announcement(albumData);
                _cache.Remove("latestAlbum");
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(_dbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(_dbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var album = await _dbContext.Albums.
                Where(a => a.AlbumId == id).
                FirstOrDefaultAsync();

            if (album == null)
            {
                return View(album);
            }

            ViewBag.GenreId = new SelectList(_dbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(_dbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Album album)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Entry(album).SetState(EntityState.Modified);
                await _dbContext.SaveChangesAsync(Context.RequestAborted);
                //Invalidate the cache entry as it is modified
                _cache.Remove(GetCacheKey(album.AlbumId));
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(_dbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(_dbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/RemoveAlbum/5
        public async Task<IActionResult> RemoveAlbum(int id)
        {
            var album = await _dbContext.Albums.Where(a => a.AlbumId == id).FirstOrDefaultAsync();
            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public async Task<IActionResult> RemoveAlbumConfirmed(int id)
        {
            var album = await _dbContext.Albums.Where(a => a.AlbumId == id).FirstOrDefaultAsync();

            if (album != null)
            {
                _dbContext.Albums.Remove(album);
                await _dbContext.SaveChangesAsync(Context.RequestAborted);
                //Remove the cache entry as it is removed
                _cache.Remove(GetCacheKey(id));
            }

            return RedirectToAction("Index");
        }

        private static string GetCacheKey(int id)
        {
            return string.Format("album_{0}", id);
        }

#if TESTING
        //
        // GET: /StoreManager/GetAlbumIdFromName
        // Note: Added for automated testing purpose. Application does not use this.
        [HttpGet]
        public async Task<IActionResult> GetAlbumIdFromName(string albumName)
        {
            var album = await _dbContext.Albums.Where(a => a.Title == albumName).FirstOrDefaultAsync();

            if (album == null)
            {
                return HttpNotFound();
            }

            return new ContentResult { Content = album.AlbumId.ToString(), ContentType = "text/plain" };
        }
#endif
    }
}
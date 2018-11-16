using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicStore.Models;
using MusicStore.ViewModels;

namespace MusicStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("ManageStore")]
    public class StoreManagerController : Controller
    {
        private readonly AppSettings _appSettings;

        public StoreManagerController(MusicStoreContext dbContext, IOptions<AppSettings> options)
        {
            DbContext = dbContext;
            _appSettings = options.Value;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /StoreManager/
        public async Task<IActionResult> Index()
        {
            var albums = await DbContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .ToListAsync();

            return View(albums);
        }

        //
        // GET: /StoreManager/Details/5
        public async Task<IActionResult> Details(
            [FromServices] IMemoryCache cache,
            int id)
        {
            var cacheKey = GetCacheKey(id);

            Album album;
            if (!cache.TryGetValue(cacheKey, out album))
            {
                album = await DbContext.Albums
                        .Where(a => a.AlbumId == id)
                        .Include(a => a.Artist)
                        .Include(a => a.Genre)
                        .FirstOrDefaultAsync();

                if (album != null)
                {
                    if (_appSettings.CacheDbResults)
                    {
                        //Remove it from cache if not retrieved in last 10 minutes.
                        cache.Set(
                            cacheKey,
                            album,
                            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                    }
                }
            }

            if (album == null)
            {
                cache.Remove(cacheKey);
                return NotFound();
            }

            return View(album);
        }

        //
        // GET: /StoreManager/Create
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(DbContext.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(DbContext.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Album album,
            [FromServices] IMemoryCache cache,
            CancellationToken requestAborted)
        {
            if (ModelState.IsValid)
            {
                DbContext.Albums.Add(album);
                await DbContext.SaveChangesAsync(requestAborted);

                var albumData = new AlbumData
                {
                    Title = album.Title,
                    Url = Url.Action("Details", "Store", new { id = album.AlbumId })
                };

                cache.Remove("latestAlbum");
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(DbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(DbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var album = await DbContext.Albums.
                Where(a => a.AlbumId == id).
                FirstOrDefaultAsync();

            if (album == null)
            {
                return NotFound();
            }

            ViewBag.GenreId = new SelectList(DbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(DbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [FromServices] IMemoryCache cache,
            Album album,
            CancellationToken requestAborted)
        {
            if (ModelState.IsValid)
            {
                DbContext.Update(album);
                await DbContext.SaveChangesAsync(requestAborted);
                //Invalidate the cache entry as it is modified
                cache.Remove(GetCacheKey(album.AlbumId));
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(DbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(DbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/RemoveAlbum/5
        public async Task<IActionResult> RemoveAlbum(int id)
        {
            var album = await DbContext.Albums.Where(a => a.AlbumId == id).FirstOrDefaultAsync();
            if (album == null)
            {
                return NotFound();
            }

            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public async Task<IActionResult> RemoveAlbumConfirmed(
            [FromServices] IMemoryCache cache,
            int id,
            CancellationToken requestAborted)
        {
            var album = await DbContext.Albums.Where(a => a.AlbumId == id).FirstOrDefaultAsync();
            if (album == null)
            {
                return NotFound();
            }

            DbContext.Albums.Remove(album);
            await DbContext.SaveChangesAsync(requestAborted);
            //Remove the cache entry as it is removed
            cache.Remove(GetCacheKey(id));

            return RedirectToAction("Index");
        }

        private static string GetCacheKey(int id)
        {
            return string.Format("album_{0}", id);
        }

        // NOTE: this is used for end to end testing only
        //
        // GET: /StoreManager/GetAlbumIdFromName
        // Note: Added for automated testing purpose. Application does not use this.
        [HttpGet]
        [SkipStatusCodePages]
        [EnableCors("CorsPolicy")]
        public async Task<IActionResult> GetAlbumIdFromName(string albumName)
        {
            var album = await DbContext.Albums.Where(a => a.Title == albumName).FirstOrDefaultAsync();

            if (album == null)
            {
                return NotFound();
            }

            return Content(album.AlbumId.ToString());
        }
    }
}
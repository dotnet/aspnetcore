using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Data.Entity;
using Microsoft.Framework.Caching.Memory;
using MusicStore.Hubs;
using MusicStore.Models;
using MusicStore.ViewModels;

namespace MusicStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNet.Authorization.Authorize("ManageStore")]
    public class StoreManagerController : Controller
    {
        private IConnectionManager _connectionManager;
        private IHubContext _announcementHub;

        [FromServices]
        public MusicStoreContext DbContext { get; set; }

        [FromServices]
        public IMemoryCache Cache { get; set; }

        [FromServices]
        public IConnectionManager ConnectionManager
        {
            get
            {
                return _connectionManager;
            }
            set
            {
                _connectionManager = value;
                _announcementHub = _connectionManager.GetHubContext<AnnouncementHub>();
            }
        }

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
        public async Task<IActionResult> Details(int id)
        {
            var cacheKey = GetCacheKey(id);

            Album album;
            if(!Cache.TryGetValue(cacheKey, out album))
            {
                album = await DbContext.Albums
                        .Where(a => a.AlbumId == id)
                        .Include(a => a.Artist)
                        .Include(a => a.Genre)
                        .FirstOrDefaultAsync();

                if (album != null)
                {
                    //Remove it from cache if not retrieved in last 10 minutes.
                    Cache.Set(
                        cacheKey,
                        album,
                        new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                }
            }

            if (album == null)
            {
                Cache.Remove(cacheKey);
                return HttpNotFound();
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
        public async Task<IActionResult> Create(Album album, CancellationToken requestAborted)
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

                _announcementHub.Clients.All.announcement(albumData);
                Cache.Remove("latestAlbum");
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
                return HttpNotFound();
            }

            ViewBag.GenreId = new SelectList(DbContext.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(DbContext.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Album album, CancellationToken requestAborted)
        {
            if (ModelState.IsValid)
            {
                DbContext.Update(album);
                await DbContext.SaveChangesAsync(requestAborted);
                //Invalidate the cache entry as it is modified
                Cache.Remove(GetCacheKey(album.AlbumId));
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
                return HttpNotFound();
            }

            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public async Task<IActionResult> RemoveAlbumConfirmed(int id, CancellationToken requestAborted)
        {
            var album = await DbContext.Albums.Where(a => a.AlbumId == id).FirstOrDefaultAsync();
            if (album == null)
            {
                return HttpNotFound();
            }

            DbContext.Albums.Remove(album);
            await DbContext.SaveChangesAsync(requestAborted);
            //Remove the cache entry as it is removed
            Cache.Remove(GetCacheKey(id));

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
        [SkipStatusCodePages]
        [EnableCors("CorsPolicy")]
        public async Task<IActionResult> GetAlbumIdFromName(string albumName)
        {
            var album = await DbContext.Albums.Where(a => a.Title == albumName).FirstOrDefaultAsync();

            if (album == null)
            {
                return HttpNotFound();
            }

            return new ContentResult { Content = album.AlbumId.ToString() };
        }
#endif
    }
}
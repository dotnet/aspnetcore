using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Data.Entity;
using MusicStore.Models;
using System.Linq;
using MusicStore.Hubs;
using MusicStore.ViewModels;
using Microsoft.Framework.Cache.Memory;
using System;
using System.Threading.Tasks;

namespace MusicStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNet.Mvc.Authorize("ManageStore", "Allowed")]
    public class StoreManagerController : Controller
    {
        private readonly MusicStoreContext db;
        private IHubContext annoucementHub;
        private readonly IMemoryCache cache;

        public StoreManagerController(MusicStoreContext context, IConnectionManager connectionManager, IMemoryCache memoryCache)
        {
            db = context;
            annoucementHub = connectionManager.GetHubContext<AnnouncementHub>();
            cache = memoryCache;
        }

        //
        // GET: /StoreManager/

        public IActionResult Index()
        {
            // TODO [EF] Swap to native support for loading related data when available
            var albums = from album in db.Albums
                         join genre in db.Genres on album.GenreId equals genre.GenreId
                         join artist in db.Artists on album.ArtistId equals artist.ArtistId
                         select new Album()
                         {
                             ArtistId = album.ArtistId,
                             AlbumArtUrl = album.AlbumArtUrl,
                             AlbumId = album.AlbumId,
                             GenreId = album.GenreId,
                             Price = album.Price,
                             Title = album.Title,
                             Artist = new Artist()
                             {
                                 ArtistId = album.ArtistId,
                                 Name = artist.Name
                             },
                             Genre = new Genre()
                             {
                                 GenreId = album.GenreId,
                                 Name = genre.Name
                             }
                         };

            return View(albums);
        }

        //
        // GET: /StoreManager/Details/5

        public IActionResult Details(int id)
        {
            string cacheId = string.Format("album_{0}", id);
            var album = cache.GetOrSet(cacheId, context =>
            {
                //Remove it from cache if not retrieved in last 10 minutes
                context.SetSlidingExpiration(TimeSpan.FromMinutes(10));
                //If this returns null how do we prevent the cache to store this. 
                return db.Albums.Where(a => a.AlbumId == id).FirstOrDefault();
            });

            if (album == null)
            {
                cache.Remove(cacheId);
                return View(album);
            }

            // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
            album.Genre = db.Genres.Single(g => g.GenreId == album.GenreId);
            album.Artist = db.Artists.Single(a => a.ArtistId == album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Create
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Album album)
        {
            if (ModelState.IsValid)
            {
                await db.Albums.AddAsync(album, Context.RequestAborted);
                await db.SaveChangesAsync(Context.RequestAborted);
                annoucementHub.Clients.All.announcement(new AlbumData() { Title = album.Title, Url = Url.Action("Details", "Store", new { id = album.AlbumId }) });
                cache.Remove("latestAlbum");
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5
        public IActionResult Edit(int id)
        {
            Album album = db.Albums.Where(a => a.AlbumId == id).FirstOrDefault();

            if (album == null)
            {
                return View(album);
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
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
                db.Entry(album).State = EntityState.Modified;
                await db.SaveChangesAsync(Context.RequestAborted);
                //Invalidate the cache entry as it is modified
                cache.Remove(string.Format("album_{0}", album.AlbumId));
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/RemoveAlbum/5
        public IActionResult RemoveAlbum(int id)
        {
            Album album = db.Albums.Where(a => a.AlbumId == id).FirstOrDefault();
            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public async Task<IActionResult> RemoveAlbumConfirmed(int id)
        {
            Album album = db.Albums.Where(a => a.AlbumId == id).FirstOrDefault();

            if (album != null)
            {
                db.Albums.Remove(album);
                await db.SaveChangesAsync(Context.RequestAborted);
                //Remove the cache entry as it is removed
                cache.Remove(string.Format("album_{0}", id));
            }

            return RedirectToAction("Index");
        }

#if TESTING
        //
        // GET: /StoreManager/GetAlbumIdFromName
        // Note: Added for automated testing purpose. Application does not use this.
        [HttpGet]
        public IActionResult GetAlbumIdFromName(string albumName)
        {
            var album = db.Albums.Where(a => a.Title == albumName).FirstOrDefault();

            if (album == null)
            {
                return HttpNotFound();
            }

            return new ContentResult { Content = album.AlbumId.ToString(), ContentType = "text/plain" };
        }
#endif
    }
}
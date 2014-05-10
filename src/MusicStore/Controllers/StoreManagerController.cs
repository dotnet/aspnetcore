using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using MusicStore.Models;
using System.Linq;

namespace MusicStore.Controllers
{
    [Authorize("ManageStore", "Allowed")]
    public class StoreManagerController : Controller
    {
        private readonly MusicStoreContext db;

        public StoreManagerController(MusicStoreContext context)
        {
            db = context;
        }

        //
        // GET: /StoreManager/

        public IActionResult Index()
        {
            // TODO [EF] Swap to native support for loading related data when available
            var albums = db.Albums;
            foreach (var album in albums)
            {
                album.Genre = db.Genres.Single(g => g.GenreId == album.GenreId);
                album.Artist = db.Artists.Single(a => a.ArtistId == album.ArtistId);
            }

            return View(albums.ToList());
        }

        //
        // GET: /StoreManager/Details/5

        public IActionResult Details(int id = 0)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            
            if (album == null)
            {
                return new HttpStatusCodeResult(404);
            }

            // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
            album.Genre = db.Genres.Single(g => g.GenreId == album.GenreId);
            album.Artist = db.Artists.Single(a => a.ArtistId == album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Create
        //Bug: https://github.com/aspnet/WebFx/issues/339
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        public IActionResult Create(Album album)
        {
            if (ModelState.IsValid)
            {
                // TODO [EF] Swap to store generated identity key when supported
                var nextId = db.Albums.Any()
                    ? db.Albums.Max(o => o.AlbumId) + 1
                    : 1;

                album.AlbumId = nextId;
                db.Albums.Add(album);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5
        //Bug: https://github.com/aspnet/WebFx/issues/339
        [HttpGet]
        public IActionResult Edit(int id = 0)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);

            if (album == null)
            {
                return new HttpStatusCodeResult(404);
            }
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        [HttpPost]
        public IActionResult Edit(Album album)
        {
            if (ModelState.IsValid)
            {
                db.ChangeTracker.Entry(album).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Delete/5

        //Bug: https://github.com/aspnet/WebFx/issues/339
        [HttpGet]
        public IActionResult Delete(int id = 0)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            if (album == null)
            {
                return new HttpStatusCodeResult(404);
            }
            return View(album);
        }

        //
        // POST: /StoreManager/Delete/5
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            // TODO [EF] Replace with DbSet.Remove when querying attaches instances
            db.ChangeTracker.Entry(album).State = EntityState.Deleted;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNet.Mvc;
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
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Album album)
        {
            if (ModelState.IsValid)
            {
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
        [ValidateAntiForgeryToken]
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
        // GET: /StoreManager/RemoveAlbum/5
        public IActionResult RemoveAlbum(int id = 0)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            if (album == null)
            {
                return new HttpStatusCodeResult(404);
            }
            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public IActionResult RemoveAlbumConfirmed(int id)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            db.Albums.Remove(album);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // GET: /StoreManager/GetAlbumIdFromName
        // Note: Added for automated testing purpose. Application does not use this.
        [HttpGet]
        public IActionResult GetAlbumIdFromName(string albumName)
        {
            var album = db.Albums.Where(a => a.Title == albumName).FirstOrDefault();

            if (album == null)
            {
                return new HttpStatusCodeResult(404);
            }

            return new ContentResult { Content = album.AlbumId.ToString(), ContentType = "text/plain" };
        }
    }
}
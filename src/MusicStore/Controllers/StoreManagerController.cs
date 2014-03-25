// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Data.Entity;
using MusicStore.Models;
using System.Linq;

namespace MusicStore.Controllers
{
    ///Bug: No Authorize attribute
    //[Authorize(Roles="Administrator")]
    public class StoreManagerController : Controller
    {
        private MusicStoreContext db = new MusicStoreContext();

        //
        // GET: /StoreManager/

        public IActionResult Index()
        {
            //Bug: Include needs EF.
            //var albums = db.Albums.Include(a => a.Genre).Include(a => a.Artist)
            //    .OrderBy(a => a.Price);

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
            //Bug: Find needs EF
            //Album album = db.Albums.Find(id);
            Album album = db.Albums.Single(a => a.AlbumId == id);
            if (album == null)
            {
                //Bug: Need method HttpNotFound() on Controller
                //return HttpNotFound();
                return this.HttpNotFound();
            }
            return View(album);
        }

        //Bug: SelectList still not available
        //
        // GET: /StoreManager/Create

        //public IActionResult Create()
        //{
        //    ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
        //    ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
        //    return View();
        //}

        //Bug: ModelState.IsValid not available
        //Bug: RedirectToAction() not available
        //Bug: SelectList not available
        // POST: /StoreManager/Create

        //[HttpPost]
        //public IActionResult Create(Album album)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Albums.Add(album);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
        //    ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
        //    return View(album);
        //}

        //
        // GET: /StoreManager/Edit/5

        public IActionResult Edit(int id = 0)
        {
            //Bug: Need EF to implement Find
            //Album album = db.Albums.Find(id);
            Album album = db.Albums.Single(a => a.AlbumId == id);

            if (album == null)
            {
                //Bug: Need method HttpNotFound() on Controller
                //return HttpNotFound();
                return this.HttpNotFound();
            }
            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        //Bug: Http actions not available
        //[HttpPost]
        public IActionResult Edit(Album album)
        {
            //Bug: ModelState.IsValid missing
            //if (ModelState.IsValid)
            {
                db.ChangeTracker.Entry(album).State = EntityState.Modified;
                db.SaveChanges();
                //Bug: Missing RedirectToAction helper
                //return RedirectToAction("Index");
            }
            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Delete/5

        public IActionResult Delete(int id = 0)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            if (album == null)
            {
                //Bug: Missing Helper
                return this.HttpNotFound();
            }
            return View(album);
        }

        //
        // POST: /StoreManager/Delete/5
        //Bug: Missing HTTP verb & action name. ActionName out of scope for alpha - So fixing the name of method in code
        //[HttpPost, ActionName("Delete")]
        //TODO: How to have an action with same name 'Delete'??
        public IActionResult DeleteConfirmed(int id)
        {
            Album album = db.Albums.Single(a => a.AlbumId == id);
            // TODO [EF] Replace with EntitySet.Remove when querying attaches instances
            db.ChangeTracker.Entry(album).State = EntityState.Deleted;
            db.SaveChanges();
            //Bug: Missing helper
            //return RedirectToAction("Index");

            return View();
        }

        //Bug: Can't dispose db. 
        //protected override void Dispose(bool disposing)
        //{
        //    db.Dispose();
        //    base.Dispose(disposing);
        //}
    }

    /// <summary>
    /// Bug: HttpNotFoundResult not available in Controllers. Work around.
    /// </summary>
    public class HttpNotFoundResult : HttpStatusCodeResult
    {
        public HttpNotFoundResult()
            : base(404)
        {

        }
    }

    /// <summary>
    /// Bug: HttpNotFoundResult not available in Controllers. Work around.
    /// </summary>
    public static class Extensions
    {
        public static IActionResult HttpNotFound(this Controller controller)
        {
            return new HttpNotFoundResult();
        }
    }
}
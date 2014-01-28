using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using MvcMusicStore.Models;

namespace MvcMusicStore.Controllers
{
    // [Authorize(Roles = "Administrator")]
    public class StoreManagerController : Controller
    {
        private MusicStoreEntities db = new MusicStoreEntities();

        //
        // GET: /StoreManager/

        public IActionResult Index()
        {
            var albums = db.Albums.Include(a => a.Genre).Include(a => a.Artist)
                .OrderBy(a => a.Price);
            return View(albums.ToList());
        }

        //
        // GET: /StoreManager/Details/5

        public IActionResult Details(int id = 0)
        {
            Album album = db.Albums.Find(id);
            if (album == null)
            {
                //return HttpNotFound();
                return null;
            }
            return View(album);
        }

        //
        // GET: /StoreManager/Create

        public IActionResult Create()
        {
            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
            return View();
        }

        //
        // POST: /StoreManager/Create

        // [HttpPost]
        public IActionResult Create(Album album)
        {
            if (/*ModelState.IsValid*/true)
            {
                db.Albums.Add(album);
                db.SaveChanges();
                // return RedirectToAction("Index");
                return null;
            }

            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5

        public IActionResult Edit(int id = 0)
        {
            Album album = db.Albums.Find(id);
            if (album == null)
            {
                // return HttpNotFound();
                return null;
            }
            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5

        // [HttpPost]
        public IActionResult Edit(Album album)
        {
            //if (ModelState.IsValid)
            //{
            //    db.Entry(album).State = EntityState.Modified;
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            //}
            //ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            //ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Delete/5

        public IActionResult Delete(int id = 0)
        {
            Album album = db.Albums.Find(id);
            if (album == null)
            {
                // return HttpNotFound();
                return null;
            }
            return View(album);
        }

        //
        // POST: /StoreManager/Delete/5

        // [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            Album album = db.Albums.Find(id);
            db.Albums.Remove(album);
            db.SaveChanges();
            // return RedirectToAction("Index");
            return null;
        }

        protected /*override*/ void Dispose(bool disposing)
        {
            db.Dispose();
            // base.Dispose(disposing);
        }
    }
}
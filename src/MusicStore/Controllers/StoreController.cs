using MvcMusicStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNet.Mvc;

namespace MvcMusicStore.Controllers
{
    public class StoreController : Controller
    {
        MusicStoreEntities storeDB = new MusicStoreEntities();
        //
        // GET: /Store/

        public IActionResult Index()
        {
            var genres = storeDB.Genres.ToList();

            return View(genres);
        }


        //
        // GET: /Store/Browse?genre=Disco

        public IActionResult Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database
            var genreModel = storeDB.Genres.Include("Albums")
                .Single(g => g.Name == genre);

            return View(genreModel);
        }

        public IActionResult Details(int id)
        {
            var album = storeDB.Albums.Find(id);

            return View(album);
        }

        // [ChildActionOnly]
        public IActionResult GenreMenu()
        {
            var genres = storeDB.Genres
                .OrderByDescending(
                    g => g.Albums.Sum(
                    a => a.OrderDetails.Sum(
                    od => od.Quantity)))
                .Take(9)
                .ToList();

            //return PartialView(genres);
            return null;
        }
    }
}
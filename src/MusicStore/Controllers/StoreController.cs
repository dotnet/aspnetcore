// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Linq;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        //Bug: Need to remove singleton instance after EF is implemented. 
        //MusicStoreEntities storeDB = new MusicStoreEntities();
        MusicStoreEntities storeDB = MusicStoreEntities.Instance;
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
            //Bug: Include is part of EF. We need to work around this temporarily
            //var genreModel = storeDB.Genres.Include("Albums")
            //    .Single(g => g.Name == genre);

            var genreModel = storeDB.Genres.Single(g => g.Name == genre);
            genreModel.Albums = storeDB.Albums.Where(a => a.GenreId == genreModel.GenreId).ToList();

            return View(genreModel);
        }

        public IActionResult Details(int id)
        {
            //Bug: Need Find method from EF. 
            //var album = storeDB.Albums.Find(id);
            var album = storeDB.Albums.Single(a => a.AlbumId == id);

            return View(album);
        }

        ///Bug: Missing [ChildActionOnly] attribute
        //[ChildActionOnly]
        public IActionResult GenreMenu()
        {
            var genres = storeDB.Genres
                .OrderByDescending(
                    g => g.Albums.Sum(
                    a => a.OrderDetails.Sum(
                    od => od.Quantity)))
                .Take(9)
                .ToList();

            //Bug: Missing PartialView method.
            //return PartialView(genres);
            return View();
        }
    }
}
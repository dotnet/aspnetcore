// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Collections.Generic;
using System.Linq;

namespace MvcMusicStore.Controllers
{
    public class HomeController : Controller
    {
        /// Bug: Hacking to return a singleton. Should go away once we have EF. 
        //private MusicStoreEntities storeDB = new MusicStoreEntities();
        private MusicStoreEntities storeDB = MusicStoreEntities.Instance;
        //
        // GET: /Home/
        public IActionResult Index()
        {
            // Get most popular albums
            var albums = GetTopSellingAlbums(6);

            return View(albums);
        }

        private List<Album> GetTopSellingAlbums(int count)
        {
            // Group the order details by album and return
            // the albums with the highest count

            return storeDB.Albums
                .OrderByDescending(a => a.OrderDetails.Count())
                .Take(count)
                .ToList();
        }
    }
}
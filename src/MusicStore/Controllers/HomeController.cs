using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Collections.Generic;
using System.Linq;

namespace MvcMusicStore.Controllers
{
    public class HomeController : Controller
    {
        private MusicStoreContext db = new MusicStoreContext();

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

            // TODO [EF] We don't query related data as yet, so the OrderByDescending isn't doing anything
            return db.Albums
                .OrderByDescending(a => a.OrderDetails.Count())
                .Take(count)
                .ToList();
        }
    }
}
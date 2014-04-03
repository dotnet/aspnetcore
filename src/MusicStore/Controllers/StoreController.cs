using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Linq;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        MusicStoreContext db = new MusicStoreContext();

        //
        // GET: /Store/

        public IActionResult Index()
        {
            var genres = db.Genres.ToList();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco

        public IActionResult Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database

            // TODO [EF] Swap to native support for loading related data when available
            var genreModel = db.Genres.Single(g => g.Name == genre);
            genreModel.Albums = db.Albums.Where(a => a.GenreId == genreModel.GenreId).ToList();

            return View(genreModel);
        }

        public IActionResult Details(int id)
        {
            var album = db.Albums.Single(a => a.AlbumId == id);

            return View(album);
        }

        ///Bug: Missing [ChildActionOnly] attribute
        //[ChildActionOnly]
        public IActionResult GenreMenu()
        {
            // TODO [EF] We don't query related data as yet, so the OrderByDescending isn't doing anything
            var genres = db.Genres
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
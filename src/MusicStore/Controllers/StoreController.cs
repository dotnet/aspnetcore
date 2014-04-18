using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Linq;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly MusicStoreContext db;

        public StoreController(MusicStoreContext context)
        {
            db = context;
        }

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

            // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
            album.Genre = db.Genres.Single(g => g.GenreId == album.GenreId);
            album.Artist = db.Artists.Single(a => a.ArtistId == album.ArtistId);

            return View(album);
        }
    }
}
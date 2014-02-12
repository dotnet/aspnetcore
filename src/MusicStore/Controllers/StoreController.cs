using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using MvcMusicStore.Models;

namespace MvcMusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly MusicStoreEntities _storeContext = new MusicStoreEntities();

        // GET: /Store/
        public async Task<IActionResult> Index()
        {
            return View(await _storeContext.Genres.ToListAsync());
        }

        // GET: /Store/Browse?genre=Disco
        public async Task<IActionResult> Browse(string genre)
        {
            return View(await _storeContext.Genres.Include(e => e.Albums).SingleAsync(g => g.Name == genre));
        }

        public async Task<IActionResult> Details(int id)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(a => a.AlbumId == id);

            return album != null ? View(album) : (IActionResult)null;//HttpNotFound();
        }

        //[ChildActionOnly]
        public IActionResult GenreMenu()
        {
            var genres = _storeContext.Genres
                .OrderByDescending(
                    g => g.Albums.Sum(
                        a => a.OrderDetails.Sum(
                            od => od.Quantity)))
                .Take(9)
                .ToList();

            return null; //PartialView(genres);
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        _storeContext.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
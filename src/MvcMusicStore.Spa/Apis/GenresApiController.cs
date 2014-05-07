using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MvcMusicStore.Models;

namespace MvcMusicStore.Apis
{
    public class GenresApiController : Controller
    {
        private readonly MusicStoreEntities _storeContext = new MusicStoreEntities();

        [Route("api/genres/lookup")]
        public ActionResult Lookup()
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Genres.Select(g => new { g.GenreId, g.Name })
            };
        }

        [Route("api/genres/menu")]
        public ActionResult GenreMenuList(int count = 9)
        {
            count = count > 0 && count < 20 ? count : 9;

            return new SmartJsonResult
            {
                Data = _storeContext.Genres
                    .OrderByDescending(g => g.Albums.Sum(a => a.OrderDetails.Sum(od => od.Quantity)))
                    .Take(count)
            };
        }

        [Route("api/genres")]
        public ActionResult GenreList()
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Genres
                    .Include(g => g.Albums)
                    .OrderBy(g => g.Name)
            };
        }

        [Route("api/genres/{genreId:int}/albums")]
        public ActionResult GenreAlbums(int genreId)
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Albums
                    .Where(a => a.GenreId == genreId)
                    .Include(a => a.Genre)
                    .Include(a => a.Artist)
                    .OrderBy(a => a.Genre.Name)
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _storeContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
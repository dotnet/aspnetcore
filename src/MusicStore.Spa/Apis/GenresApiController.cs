using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using MusicStore.Models;
using MusicStore.Spa.Infrastructure;

namespace MusicStore.Apis
{
    [Route("api/genres")]
    public class GenresApiController : Controller
    {
        private readonly MusicStoreContext _storeContext;

        public GenresApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        [HttpGet]
        public async Task<ActionResult> GenreList()
        {
            var genres = await _storeContext.Genres
                //.Include(g => g.Albums)
                .OrderBy(g => g.Name)
                .ToListAsync();

            return Json(genres);
        }

        [HttpGet("lookup")]
        public async Task<ActionResult> Lookup()
        {
            var genres = await _storeContext.Genres
                .Select(g => new { g.GenreId, g.Name })
                .ToListAsync();

            return Json(genres);
        }

        [HttpGet("menu")]
        public async Task<ActionResult> GenreMenuList(int count = 9)
        {
            count = count > 0 && count < 20 ? count : 9;

            var genres = await _storeContext.Genres
                .OrderByDescending(g =>
                    g.Albums.Sum(a =>
                        a.OrderDetails.Sum(od => od.Quantity)))
                .Take(count)
                .ToListAsync();

            return Json(genres);
        }

        [HttpGet("{genreId:int}/albums")]
        [NoCache]
        public async Task<ActionResult> GenreAlbums(int genreId)
        {
            var albums = await _storeContext.Albums
                .Where(a => a.GenreId == genreId)
                //.Include(a => a.Genre)
                //.Include(a => a.Artist)
                //.OrderBy(a => a.Genre.Name)
                .ToListAsync();

            return Json(albums);
        }
    }
}
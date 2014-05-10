using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Apis
{
    public class GenresApiController : BaseController
    {
        private readonly MusicStoreContext _storeContext;

        public GenresApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        //[Route("api/genres/lookup")]
        public async Task<ActionResult> Lookup()
        {
            return new SmartJsonResult
            {
                Data = await _storeContext.Genres
                    .Select(g => new { g.GenreId, g.Name })
                    .ToListAsync()
            };
        }

        //[Route("api/genres/menu")]
        public async Task<ActionResult> GenreMenuList(int count = 9)
        {
            count = count > 0 && count < 20 ? count : 9;

            return new SmartJsonResult
            {
                Data = await _storeContext.Genres
                    .OrderByDescending(g => g.Albums.Sum(a => a.OrderDetails.Sum(od => od.Quantity)))
                    .Take(count)
                    .ToListAsync()
            };
        }

        //[Route("api/genres")]
        public async Task<ActionResult> GenreList()
        {
            return new SmartJsonResult
            {
                Data = await _storeContext.Genres
                    .Include(g => g.Albums)
                    .OrderBy(g => g.Name)
                    .ToListAsync()
            };
        }

        //[Route("api/genres/{genreId:int}/albums")]
        public async Task<ActionResult> GenreAlbums(int genreId)
        {
            var albums = await _storeContext.Albums
                .Where(a => a.GenreId == genreId)
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                //.OrderBy(a => a.Genre.Name)
                .ToListAsync();

            return new SmartJsonResult
            {
                Data = albums
            };
        }
    }
}
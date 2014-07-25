using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Apis
{
    public class ArtistsApiController : Controller
    {
        private readonly MusicStoreContext _storeContext;

        public ArtistsApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        //[Route("api/artists/lookup")]
        public async Task<ActionResult> Lookup()
        {
            var artists = await _storeContext.Artists
                .OrderBy(a => a.Name)
                .ToListAsync();

            return Json(artists);
        }
    }
}
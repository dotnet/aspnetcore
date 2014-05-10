using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Apis
{
    public class ArtistsApiController : BaseController
    {
        private readonly MusicStoreContext _storeContext;

        public ArtistsApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        //[Route("api/artists/lookup")]
        public async Task<ActionResult> Lookup()
        {
            return new SmartJsonResult
            {
                Data = await _storeContext.Artists.OrderBy(a => a.Name).ToListAsync()
            };
        }
    }
}
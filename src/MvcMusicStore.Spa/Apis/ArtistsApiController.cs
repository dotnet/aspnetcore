using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcMusicStore.Models;

namespace MvcMusicStore.Apis
{
    public class ArtistsApiController : Controller
    {
        private readonly MusicStoreEntities _storeContext = new MusicStoreEntities();

        [Route("api/artists/lookup")]
        public ActionResult Lookup()
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Artists.OrderBy(a => a.Name).ToList()
            };
        }
    }
}
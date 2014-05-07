using System.Data.Entity;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
using MvcMusicStore.Infrastructure;
using MvcMusicStore.Models;

namespace MvcMusicStore.Apis
{
    public class AlbumsApiController : Controller
    {
        private readonly MusicStoreEntities _storeContext = new MusicStoreEntities();

        [Route("api/albums")]
        public ActionResult Paged(int page = 1, int pageSize = 50, string sortBy = null)
        {
            var pagedAlbums = _storeContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .SortBy(sortBy, a => a.Title)
                .ToPagedList(page, pageSize);

            return new SmartJsonResult
            {
                Data = pagedAlbums
            };
        }

        [Route("api/albums/all")]
        public ActionResult All()
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Albums
                    .Include(a => a.Genre)
                    .Include(a => a.Artist)
                    .OrderBy(a => a.Title)
            };
        }

        [Route("api/albums/mostPopular")]
        public ActionResult MostPopular(int count = 6)
        {
            count = count > 0 && count < 20 ? count : 6;

            return new SmartJsonResult
            {
                Data = _storeContext.Albums
                    .OrderByDescending(a => a.OrderDetails.Count())
                    .Take(count)
            };
        }

        [Route("api/albums/{albumId:int}")]
        public ActionResult Details(int albumId)
        {
            return new SmartJsonResult
            {
                Data = _storeContext.Albums
                    .Include(a => a.Artist)
                    .Include(a => a.Genre)
                    .SingleOrDefault(a => a.AlbumId == albumId)
            };
        }

        [Route("api/albums")]
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult CreateAlbum()
        {
            var album = new Album();

            if (!TryUpdateModel(album, prefix: null, includeProperties: null, excludeProperties: new[] { "Genre", "Artist", "OrderDetails" }))
            {
                // Return the model errors
                return new ApiResult(ModelState);
            }

            // Save the changes to the DB
            _storeContext.Albums.Add(album);
            _storeContext.SaveChanges();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ApiResult
            {
                Data = album.AlbumId,
                Message = "Album created successfully."
            };
        }

        [Route("api/albums/{albumId:int}/update")]
        [HttpPut]
        [Authorize(Roles = "Administrator")]
        public ActionResult UpdateAlbum(int albumId)
        {
            var album = _storeContext.Albums.SingleOrDefault(a => a.AlbumId == albumId);

            if (album == null)
            {
                return new ApiResult
                {
                    StatusCode = 404,
                    Message = string.Format("The album with ID {0} was not found.", albumId)
                };
            }

            if (!TryUpdateModel(album, prefix: null, includeProperties: null, excludeProperties: new[] { "Genre", "Artist", "OrderDetails" }))
            {
                // Return the model errors
                return new ApiResult(ModelState);
            }

            // Save the changes to the DB
            _storeContext.SaveChanges();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ApiResult
            {
                Message = "Album updated successfully."
            };
        }

        [Route("api/albums/{albumId:int}")]
        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteAlbum(int albumId)
        {
            var album = _storeContext.Albums.SingleOrDefault(a => a.AlbumId == albumId);

            if (album != null)
            {
                _storeContext.Albums.Remove(album);

                // Save the changes to the DB
                _storeContext.SaveChanges();

                // TODO: Handle missing record, key violations, concurrency issues, etc.
            }

            return new ApiResult
            {
                Message = "Album deleted successfully."
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

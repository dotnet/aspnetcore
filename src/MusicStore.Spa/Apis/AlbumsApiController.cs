using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Infrastructure;
using MusicStore.Models;

namespace MusicStore.Apis
{
    public class AlbumsApiController : BaseController
    {
        private readonly MusicStoreContext _storeContext;

        public AlbumsApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        //[Route("api/albums")]
        public async Task<ActionResult> Paged(int page = 1, int pageSize = 50, string sortBy = null)
        {
            var pagedAlbums = await _storeContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .SortBy(sortBy, a => a.Title)
                .ToPagedListAsync(page, pageSize);

            return new SmartJsonResult
            {
                Data = pagedAlbums
            };
        }

        //[Route("api/albums/all")]
        public async Task<ActionResult> All()
        {
            return new SmartJsonResult
            {
                Data = await _storeContext.Albums
                    .Include(a => a.Genre)
                    .Include(a => a.Artist)
                    .OrderBy(a => a.Title)
                    .ToListAsync()
            };
        }

        //[Route("api/albums/mostPopular")]
        public async Task<ActionResult> MostPopular(int count = 6)
        {
            count = count > 0 && count < 20 ? count : 6;

            return new SmartJsonResult
            {
                Data = await _storeContext.Albums
                    .OrderByDescending(a => a.OrderDetails.Count())
                    .Take(count)
                    .ToListAsync()
            };
        }

        //[Route("api/albums/{albumId:int}")]
        public async Task<ActionResult> Details(int albumId)
        {
            // TODO: Remove this when EF supports related entity loading
            await _storeContext.Artists.ToListAsync();
            await _storeContext.Genres.ToListAsync();

            // TODO: Make async when EF supports SingleOrDefaultAsync
            var album = _storeContext.Albums
                .Include(a => a.Artist)
                .Include(a => a.Genre)
                .SingleOrDefault(a => a.AlbumId == albumId);

            // TODO: Add null checking and return 404 in that case

            return new SmartJsonResult
            {
                Data = album
            };
        }

        //[Route("api/albums")]
        [HttpPost]
        //[Authorize(Roles = "Administrator")]
        [Authorize(ClaimTypes.Role, "Administrator")]
        public async Task<ActionResult> CreateAlbum()
        {
            var album = new Album();

            //if (!await TryUpdateModelAsync(album, excludeProperties: new[] { "Genre", "Artist", "OrderDetails" }))
            if (!await TryUpdateModelAsync(album))
            {
                // Return the model errors
                return new ApiResult(ModelState);
            }

            // Save the changes to the DB
            await _storeContext.Albums.AddAsync(album);
            await _storeContext.SaveChangesAsync();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ApiResult
            {
                Data = album.AlbumId,
                Message = "Album created successfully."
            };
        }

        //[Route("api/albums/{albumId:int}/update")]
        [HttpPut]
        //[Authorize(Roles = "Administrator")]
        [Authorize(ClaimTypes.Role, "Administrator")]
        public async Task<ActionResult> UpdateAlbum(int albumId)
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

            //if (!TryUpdateModel(album, prefix: null, includeProperties: null, excludeProperties: new[] { "Genre", "Artist", "OrderDetails" }))
            if (!await TryUpdateModelAsync(album))
            {
                // Return the model errors
                return new ApiResult(ModelState);
            }

            // Save the changes to the DB
            await _storeContext.SaveChangesAsync();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ApiResult
            {
                Message = "Album updated successfully."
            };
        }

        //[Route("api/albums/{albumId:int}")]
        [HttpDelete]
        //[Authorize(Roles = "Administrator")]
        [Authorize(ClaimTypes.Role, "Administrator")]
        public async Task<ActionResult> DeleteAlbum(int albumId)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(a => a.AlbumId == albumId);

            if (album != null)
            {
                _storeContext.Albums.Remove(album);

                // Save the changes to the DB
                await _storeContext.SaveChangesAsync();

                // TODO: Handle missing record, key violations, concurrency issues, etc.
            }

            return new ApiResult
            {
                Message = "Album deleted successfully."
            };
        }
    }
}

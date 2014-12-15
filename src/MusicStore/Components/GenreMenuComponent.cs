using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "GenreMenu")]
    public class GenreMenuComponent : ViewComponent
    {
        private readonly MusicStoreContext _dbContext;

        public GenreMenuComponent(MusicStoreContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var genres = await GetGenres();

            return View(genres);
        }

        private async Task<List<Genre>> GetGenres()
        {
            // TODO [EF] We don't query related data as yet, so the OrderByDescending isn't doing anything
            //var genres = _dbContext.Genres
            //.OrderByDescending(
            //    g => g.Albums.Sum(
            //    a => a.OrderDetails.Sum(
            //    od => od.Quantity)))
            //.Take(9)
            //.ToList();

            return await _dbContext.Genres.Take(9).ToListAsync();
        }
    }
}
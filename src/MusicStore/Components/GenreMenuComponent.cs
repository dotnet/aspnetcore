using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "GenreMenu")]
    public class GenreMenuComponent : ViewComponent
    {
        public GenreMenuComponent(MusicStoreContext dbContext)
        {
            DbContext = dbContext;
        }

        private MusicStoreContext DbContext { get; }

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

            return await DbContext.Genres.Take(9).ToListAsync();
        }
    }
}
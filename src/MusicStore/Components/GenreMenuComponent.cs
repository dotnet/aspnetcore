using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            return await DbContext.Genres
                .Include(g => g.Albums).ThenInclude(a => a.OrderDetails)
                // TODO use nested sum https://github.com/aspnet/EntityFramework/issues/3792
                //.OrderByDescending(
                //    g => g.Albums.Sum(a => a.OrderDetails.Sum(od => od.Quantity)))
                .Take(9)
                .ToListAsync();
        }
    }
}
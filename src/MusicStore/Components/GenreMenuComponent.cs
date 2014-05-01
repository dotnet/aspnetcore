using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Collections.Generic;

namespace MusicStore.Components
{
    [ViewComponent(Name = "GenreMenu")]
    public class GenreMenuComponent : ViewComponent
    {
        private readonly MusicStoreContext db;

        public GenreMenuComponent(MusicStoreContext context)
        {
            db = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var genres = await GetGenres();

            return View(genres);
        }

        private Task<List<Genre>> GetGenres()
        {
            // TODO [EF] We don't query related data as yet, so the OrderByDescending isn't doing anything
            //var genres = db.Genres
            //.OrderByDescending(
            //    g => g.Albums.Sum(
            //    a => a.OrderDetails.Sum(
            //    od => od.Quantity)))
            //.Take(9)
            //.ToList();

            var genres = db.Genres.ToList();
            return Task.FromResult(genres);
        }
    }
}
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using MvcMusicStore.Models;

namespace MvcMusicStore.Controllers
{
    //[Authorize(Roles = "Administrator")]
    public class StoreManagerController : Controller
    {
        private readonly MusicStoreEntities _storeContext = new MusicStoreEntities();

        // GET: /StoreManager/
        public async Task<IActionResult> Index()
        {
            return View(await _storeContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .OrderBy(a => a.Price).ToListAsync());
        }

        // GET: /StoreManager/Details/5
        public async Task<IActionResult> Details(int id = 0)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(e => e.AlbumId == id);

            if (album == null)
            {
                return null;//HttpNotFound();
            }

            return View(album);
        }

        // GET: /StoreManager/Create
        public async Task<IActionResult> Create()
        {
            return await BuildView(null);
        }

        // POST: /StoreManager/Create
        //[HttpPost]
        public async Task<IActionResult> Create(Album album)
        {
            if (true)//ModelState.IsValid)
            {
                _storeContext.Albums.Add(album);

                await _storeContext.SaveChangesAsync();

                return null;//RedirectToAction("Index");
            }

            return await BuildView(album);
        }

        // GET: /StoreManager/Edit/5
        public async Task<IActionResult> Edit(int id = 0)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(e => e.AlbumId == id);
            if (album == null)
            {
                return null;//HttpNotFound();
            }

            return await BuildView(album);
        }

        // POST: /StoreManager/Edit/5
        //[HttpPost]
        public async Task<IActionResult> Edit(Album album)
        {
            if (true)//ModelState.IsValid)
            {
                _storeContext.Albums.Update(album);

                await _storeContext.SaveChangesAsync();

                return null;//RedirectToAction("Index");
            }

            return await BuildView(album);
        }

        // GET: /StoreManager/Delete/5
        public async Task<IActionResult> Delete(int id = 0)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(e => e.AlbumId == id);
            if (album == null)
            {
                return null;//HttpNotFound();
            }

            return View(album);
        }

        // POST: /StoreManager/Delete/5
        //[HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(e => e.AlbumId == id);
            if (album == null)
            {
                return null;//HttpNotFound();
            }

            _storeContext.Albums.Remove(album);

            await _storeContext.SaveChangesAsync();

            return null;//RedirectToAction("Index");
        }

        private async Task<IActionResult> BuildView(Album album)
        {
            //ViewBag.GenreId = new SelectList(
            //    await _storeContext.Genres.ToListAsync(),
            //    "GenreId",
            //    "Name",
            //    album == null ? null : (object)album.GenreId);

            //ViewBag.ArtistId = new SelectList(
            //    await _storeContext.Artists.ToListAsync(),
            //    "ArtistId",
            //    "Name",
            //    album == null ? null : (object)album.ArtistId);

            return View(album);
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        _storeContext.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
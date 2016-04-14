using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Models;
using MusicStore.ViewModels;

namespace MusicStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        public ShoppingCartController(MusicStoreContext dbContext)
        {
            DbContext = dbContext;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /ShoppingCart/
        public async Task<IActionResult> Index()
        {
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = await cart.GetCartItems(),
                CartTotal = await cart.GetTotal()
            };

            // Return the view
            return View(viewModel);
        }

        //
        // GET: /ShoppingCart/AddToCart/5

        public async Task<IActionResult> AddToCart(int id, CancellationToken requestAborted)
        {
            // Retrieve the album from the database
            var addedAlbum = await DbContext.Albums
                .SingleAsync(album => album.AlbumId == id);

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            await cart.AddToCart(addedAlbum);

            await DbContext.SaveChangesAsync(requestAborted);

            // Go back to the main store page for more shopping
            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(
            int id,
            CancellationToken requestAborted)
        {
            // Retrieve the current user's shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Get the name of the album to display confirmation
            var cartItem = await DbContext.CartItems
                .Where(item => item.CartItemId == id)
                .Include(c => c.Album)
                .SingleOrDefaultAsync();

            // Remove from cart
            int itemCount = cart.RemoveFromCart(id);

            await DbContext.SaveChangesAsync(requestAborted);

            string removed = (itemCount > 0) ? " 1 copy of " : string.Empty;

            // Display the confirmation message

            var results = new ShoppingCartRemoveViewModel
            {
                Message = removed + cartItem.Album.Title +
                    " has been removed from your shopping cart.",
                CartTotal = await cart.GetTotal(),
                CartCount = await cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            return Json(results);
        }
    }
}
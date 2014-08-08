using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using MusicStore.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace MusicStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly MusicStoreContext db;

        public ShoppingCartController(MusicStoreContext context)
        {
            db = context;
        }

        //
        // GET: /ShoppingCart/

        public IActionResult Index()
        {
            var cart = ShoppingCart.GetCart(db, Context);

            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cart.GetCartItems(),
                CartTotal = cart.GetTotal()
            };

            // Return the view
            return View(viewModel);
        }

        //
        // GET: /ShoppingCart/AddToCart/5

        public IActionResult AddToCart(int id)
        {
            // Retrieve the album from the database
            var addedAlbum = db.Albums
                .Single(album => album.AlbumId == id);

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(db, Context);

            cart.AddToCart(addedAlbum);

            db.SaveChanges();

            // Go back to the main store page for more shopping
            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var formParameters = await Context.Request.GetFormAsync();
            var requestVerification = formParameters["RequestVerificationToken"];
            string cookieToken = null;
            string formToken = null;

            if (!string.IsNullOrWhiteSpace(requestVerification))
            {
                var tokens = requestVerification.Split(':');

                if (tokens != null && tokens.Length == 2)
                {
                    cookieToken = tokens[0];
                    formToken = tokens[1];
                }
            }

            var antiForgery = Context.RequestServices.GetService<AntiForgery>();
            antiForgery.Validate(Context, new AntiForgeryTokenSet(formToken, cookieToken));

            // Retrieve the current user's shopping cart
            var cart = ShoppingCart.GetCart(db, Context);

            // Get the name of the album to display confirmation
            // TODO [EF] Turn into one query once query of related data is enabled
            int albumId = db.CartItems.Single(item => item.CartItemId == id).AlbumId;
            string albumName = db.Albums.Single(a => a.AlbumId == albumId).Title;

            // Remove from cart
            int itemCount = cart.RemoveFromCart(id);

            db.SaveChanges();

            string removed = (itemCount > 0) ? " 1 copy of " : string.Empty;

            // Display the confirmation message

            var results = new ShoppingCartRemoveViewModel
            {
                Message = removed + albumName +
                    " has been removed from your shopping cart.",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            return Json(results);
        }
    }
}
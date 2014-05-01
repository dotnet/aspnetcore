using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Components
{
    [ViewComponent(Name = "CartSummary")]
    public class CartSummaryComponent : ViewComponent
    {
        private readonly MusicStoreContext db;

        public CartSummaryComponent(MusicStoreContext context)
        {
            db = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cartItems = await GetCartItems();

            ViewBag.CartCount = cartItems.Count();
            ViewBag.CartSummary = string.Join("\n", cartItems.Distinct());

            return View();
        }

        private Task<IOrderedEnumerable<string>> GetCartItems()
        {
            var cart = ShoppingCart.GetCart(db, this.Context);

            var cartItems = cart.GetCartItems()
                .Select(a => a.Album.Title)
                .OrderBy(x => x);

            return Task.FromResult(cartItems);
        }
    }
}
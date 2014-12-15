using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Components
{
    [ViewComponent(Name = "CartSummary")]
    public class CartSummaryComponent : ViewComponent
    {
        private readonly MusicStoreContext _dbContext;

        public CartSummaryComponent(MusicStoreContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cartItems = await GetCartItems();

            ViewBag.CartCount = cartItems.Count();
            ViewBag.CartSummary = string.Join("\n", cartItems.Distinct());

            return View();
        }

        private async Task<IOrderedEnumerable<string>> GetCartItems()
        {
            var cart = ShoppingCart.GetCart(_dbContext, Context);

            return (await cart.GetCartItems())
                .Select(a => a.Album.Title)
                .OrderBy(x => x);
        }
    }
}
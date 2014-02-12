using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MvcMusicStore.Models
{
    public class ShoppingCart
    {
        public const string CartSessionKey = "CartId";

        private readonly MusicStoreEntities _storeContext;
        private readonly string _cartId;

        private ShoppingCart(MusicStoreEntities storeContext, string cartId)
        {
            _storeContext = storeContext;
            _cartId = cartId;
        }

        public static ShoppingCart GetCart(MusicStoreEntities storeContext, Controller controller)
        {
            return new ShoppingCart(storeContext, GetCartId(controller.HttpContext));
        }

        private static string GetCartId(HttpContextBase context)
        {
            if (context.Session[CartSessionKey] == null)
            {
                var username = context.User.Identity.Name;

                context.Session[CartSessionKey] = !string.IsNullOrWhiteSpace(username)
                    ? username
                    : Guid.NewGuid().ToString();
            }

            return context.Session[CartSessionKey].ToString();
        }

        public async Task AddToCart(Album album)
        {
            var cartItem = await GetCartItem(album.AlbumId);

            if (cartItem == null)
            {
                cartItem = new Cart
                {
                    AlbumId = album.AlbumId,
                    CartId = _cartId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                _storeContext.Carts.Add(cartItem);
            }
            else
            {
                cartItem.Count++;
            }
        }

        public async Task<int> RemoveFromCart(int id)
        {
            var cartItem = await GetCartItem(id);

            if (cartItem != null)
            {
                if (cartItem.Count > 1)
                {
                    return --cartItem.Count;
                }

                _storeContext.Carts.Remove(cartItem);
            }

            return 0;
        }

        private Task<Cart> GetCartItem(int albumId)
        {
            return _storeContext.Carts.SingleOrDefaultAsync(
                c => c.CartId == _cartId && c.AlbumId == albumId);
        }

        public IQueryable<Cart> GetCartItems()
        {
            return _storeContext.Carts.Where(c => c.CartId == _cartId);
        }

        public Task<int> GetCount()
        {
            return _storeContext.Carts
                .Where(c => c.CartId == _cartId)
                .Select(c => c.Count)
                .SumAsync();
        }

        public Task<decimal> GetTotal()
        {
            return _storeContext.Carts
                .Where(c => c.CartId == _cartId)
                .Select(c => c.Count * c.Album.Price)
                .SumAsync();
        }

        public async Task<int> CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = await _storeContext.Carts
                .Where(c => c.CartId == _cartId)
                .Include(c => c.Album)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    AlbumId = item.AlbumId,
                    OrderId = order.OrderId,
                    UnitPrice = item.Album.Price,
                    Quantity = item.Count,
                });

                orderTotal += item.Count * item.Album.Price;
            }

            order.Total = orderTotal;

            await EmptyCart();

            return order.OrderId;
        }

        private async Task EmptyCart()
        {
            foreach (var cartItem in await _storeContext.Carts.Where(
                c => c.CartId == _cartId).ToListAsync())
            {
                _storeContext.Carts.Remove(cartItem);
            }
        }
        
        public async Task MigrateCart(string userName)
        {
            var carts = await _storeContext.Carts.Where(c => c.CartId == _cartId).ToListAsync();

            foreach (var item in carts)
            {
                item.CartId = userName;
            }

            await _storeContext.SaveChangesAsync();
        }
    }
}
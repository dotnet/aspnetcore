using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;

namespace MusicStore.Models
{
    public class ShoppingCart
    {
        private readonly MusicStoreContext _dbContext;
        private string ShoppingCartId { get; set; }

        public ShoppingCart(MusicStoreContext dbContext)
        {
            _dbContext = dbContext;
        }

        public static ShoppingCart GetCart(MusicStoreContext db, HttpContext context)
        {
            var cart = new ShoppingCart(db);
            cart.ShoppingCartId = cart.GetCartId(context);
            return cart;
        }

        public void AddToCart(Album album)
        {
            // Get the matching cart and album instances
            var cartItem = _dbContext.CartItems.SingleOrDefault(
                c => c.CartId == ShoppingCartId
                && c.AlbumId == album.AlbumId);

            if (cartItem == null)
            {
                // Create a new cart item if no cart item exists
                cartItem = new CartItem
                {
                    AlbumId = album.AlbumId,
                    CartId = ShoppingCartId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                _dbContext.CartItems.Add(cartItem);
            }
            else
            {
                // If the item does exist in the cart, then add one to the quantity
                cartItem.Count++;
            }
        }

        public int RemoveFromCart(int id)
        {
            // Get the cart
            var cartItem = _dbContext.CartItems.Single(
                cart => cart.CartId == ShoppingCartId
                && cart.CartItemId == id);

            int itemCount = 0;

            if (cartItem != null)
            {
                if (cartItem.Count > 1)
                {
                    cartItem.Count--;
                    itemCount = cartItem.Count;
                }
                else
                {
                    _dbContext.CartItems.Remove(cartItem);
                }
            }

            return itemCount;
        }

        public void EmptyCart()
        {
            var cartItems = _dbContext.CartItems.Where(cart => cart.CartId == ShoppingCartId).ToArray();
            _dbContext.CartItems.RemoveRange(cartItems);
        }

        public async Task<List<CartItem>> GetCartItems()
        {
            return await _dbContext.CartItems.
                Where(cart => cart.CartId == ShoppingCartId).
                Include(c => c.Album).
                ToListAsync();
        }

        public async Task<int> GetCount()
        {
            // Get the count of each item in the cart and sum them up
            return await (from cartItem in _dbContext.CartItems
                          where cartItem.CartId == ShoppingCartId
                          select cartItem.Count).SumAsync();
        }

        public async Task<decimal> GetTotal()
        {
            // Multiply album price by count of that album to get 
            // the current price for each of those albums in the cart
            // sum all album price totals to get the cart total

            // TODO: Use nav prop traversal instead of joins (EF #https://github.com/aspnet/EntityFramework/issues/325)
            return await (from cartItem in _dbContext.CartItems
                          join album in _dbContext.Albums on cartItem.AlbumId equals album.AlbumId
                          where cartItem.CartId == ShoppingCartId
                          select cartItem.Count * album.Price).SumAsync();
        }

        public async Task<int> CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = await GetCartItems();

            // Iterate over the items in the cart, adding the order details for each
            foreach (var item in cartItems)
            {
                //var album = _db.Albums.Find(item.AlbumId);
                var album = _dbContext.Albums.Single(a => a.AlbumId == item.AlbumId);

                var orderDetail = new OrderDetail
                {
                    AlbumId = item.AlbumId,
                    OrderId = order.OrderId,
                    UnitPrice = album.Price,
                    Quantity = item.Count,
                };

                // Set the order total of the shopping cart
                orderTotal += (item.Count * album.Price);

                _dbContext.OrderDetails.Add(orderDetail);
            }

            // Set the order's total to the orderTotal count
            order.Total = orderTotal;

            // Empty the shopping cart
            EmptyCart();

            // Return the OrderId as the confirmation number
            return order.OrderId;
        }

        // We're using HttpContextBase to allow access to sessions.
        private string GetCartId(HttpContext context)
        {
            var cartId = context.Session.GetString("Session");

            if (cartId == null)
            {
                //A GUID to hold the cartId. 
                cartId = Guid.NewGuid().ToString();

                // Send cart Id as a cookie to the client.
                context.Session.SetString("Session", cartId);
            }

            return cartId;
        }
    }
}
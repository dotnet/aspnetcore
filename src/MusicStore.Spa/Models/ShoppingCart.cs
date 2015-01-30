using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicStore.Models
{
    public partial class ShoppingCart
    {
        MusicStoreContext _db;
        string ShoppingCartId { get; set; }

        public ShoppingCart(MusicStoreContext db)
        {
            _db = db;
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
            var cartItem = _db.CartItems.SingleOrDefault(
                c => c.CartId == ShoppingCartId
                && c.AlbumId == album.AlbumId);

            if (cartItem == null)
            {
                // TODO [EF] Swap to store generated key once we support identity pattern
                var nextCartItemId = _db.CartItems.Any()
                    ? _db.CartItems.Max(c => c.CartItemId) + 1
                    : 1;

                // Create a new cart item if no cart item exists
                cartItem = new CartItem
                {
                    CartItemId = nextCartItemId,
                    AlbumId = album.AlbumId,
                    CartId = ShoppingCartId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                _db.CartItems.Add(cartItem);
            }
            else
            {
                // If the item does exist in the cart, then add one to the quantity
                cartItem.Count++;

                // TODO [EF] Remove this line once change detection is available
                _db.Update(cartItem);
            }
        }

        public int RemoveFromCart(int id)
        {
            // Get the cart
            var cartItem = _db.CartItems.Single(
                cart => cart.CartId == ShoppingCartId
                && cart.CartItemId == id);

            int itemCount = 0;

            if (cartItem != null)
            {
                if (cartItem.Count > 1)
                {
                    cartItem.Count--;

                    // TODO [EF] Remove this line once change detection is available
                    _db.Update(cartItem);

                    itemCount = cartItem.Count;
                }
                else
                {
                    _db.CartItems.Remove(cartItem);
                }
            }

            return itemCount;
        }

        public void EmptyCart()
        {
            var cartItems = _db.CartItems.Where(cart => cart.CartId == ShoppingCartId);

            foreach (var cartItem in cartItems)
            {
                _db.Remove(cartItem);
            }
        }

        public List<CartItem> GetCartItems()
        {
            var cartItems = _db.CartItems.Where(cart => cart.CartId == ShoppingCartId).ToList();
            //TODO: Auto population of the related album data not available until EF feature is lighted up.
            foreach (var cartItem in cartItems)
            {
                cartItem.Album = _db.Albums.Single(a => a.AlbumId == cartItem.AlbumId);
            }

            return cartItems;
        }

        public int GetCount()
        {
            // Get the count of each item in the cart and sum them up
            int? count = (from cartItems in _db.CartItems
                          where cartItems.CartId == ShoppingCartId
                          select (int?)cartItems.Count).Sum();

            // Return 0 if all entries are null
            return count ?? 0;
        }

        public decimal GetTotal()
        {
            // Multiply album price by count of that album to get 
            // the current price for each of those albums in the cart
            // sum all album price totals to get the cart total

            // TODO Collapse to a single query once EF supports querying related data
            decimal total = 0;
            foreach (var item in _db.CartItems.Where(c => c.CartId == ShoppingCartId))
            {
                var album = _db.Albums.Single(a => a.AlbumId == item.AlbumId);
                total += item.Count * album.Price;
            }

            return total;
        }

        public int CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = GetCartItems();

            // TODO [EF] Swap to store generated identity key when supported
            var nextId = _db.OrderDetails.Any()
                ? _db.OrderDetails.Max(o => o.OrderDetailId) + 1
                : 1;

            // Iterate over the items in the cart, adding the order details for each
            foreach (var item in cartItems)
            {
                //var album = _db.Albums.Find(item.AlbumId);
                var album = _db.Albums.Single(a => a.AlbumId == item.AlbumId);

                var orderDetail = new OrderDetail
                {
                    OrderDetailId = nextId,
                    AlbumId = item.AlbumId,
                    OrderId = order.OrderId,
                    UnitPrice = album.Price,
                    Quantity = item.Count,
                };

                // Set the order total of the shopping cart
                orderTotal += (item.Count * album.Price);

                _db.OrderDetails.Add(orderDetail);

                nextId++;
            }

            // Set the order's total to the orderTotal count
            order.Total = orderTotal;

            // Empty the shopping cart
            EmptyCart();

            // Return the OrderId as the confirmation number
            return order.OrderId;
        }

        // We're using HttpContextBase to allow access to cookies.
        public string GetCartId(HttpContext context)
        {
            var sessionCookie = context.Request.Cookies.Get("Session");
            string cartId = null;

            if (string.IsNullOrWhiteSpace(sessionCookie))
            {
                //A GUID to hold the cartId. 
                cartId = Guid.NewGuid().ToString();

                // Send cart Id as a cookie to the client.
                context.Response.Cookies.Append("Session", cartId);
            }
            else
            {
                cartId = sessionCookie;
            }

            return cartId;
        }
    }
}
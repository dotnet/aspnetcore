using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using MvcMusicStore.Models;

namespace MvcMusicStore.Controllers
{
    // [Authorize]
    public class CheckoutController : Controller
    {
        MusicStoreEntities storeDB = new MusicStoreEntities();
        const string PromoCode = "FREE";

        //
        // GET: /Checkout/

        public IActionResult AddressAndPayment()
        {
            return View();
        }

        //
        // POST: /Checkout/AddressAndPayment

        // [HttpPost]
        public IActionResult AddressAndPayment(IDictionary<string, string> values /*FormCollection values*/)
        {
            var order = new Order();
            // TryUpdateModel(order);

            try
            {
                if (string.Equals(values["PromoCode"], PromoCode,
                    StringComparison.OrdinalIgnoreCase) == false)
                {
                    return View(order);
                }
                else
                {
                    // order.Username = User.Identity.Name;
                    order.OrderDate = DateTime.Now;

                    //Add the Order
                    storeDB.Orders.Add(order);

                    //Process the order
                    var cart = ShoppingCart.GetCart(storeDB, this.Context);
                    cart.CreateOrder(order);

                    // Save all changes
                    storeDB.SaveChanges();

                    //return RedirectToAction("Complete",
                    //    new { id = order.OrderId });
                    return null;
                }

            }
            catch
            {
                //Invalid - redisplay with errors
                return View(order);
            }
        }

        //
        // GET: /Checkout/Complete

        public IActionResult Complete(int id)
        {
            // Validate customer owns this order
            bool isValid = storeDB.Orders.Any(
                o => o.OrderId == id &&
                o.Username == /*User.Identity.Name*/ null);

            if (isValid)
            {
                return View(id);
            }
            else
            {
                return View("Error");
            }
        }
    }
}
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System;
using System.Linq;

namespace MusicStore.Controllers
{
    //Bug: Missing auth filter
    //[Authorize]
    public class CheckoutController : Controller
    {
        //Bug: Missing EF
        //MusicStoreEntities storeDB = new MusicStoreEntities();
        MusicStoreEntities storeDB = MusicStoreEntities.Instance;
        const string PromoCode = "FREE";

        //
        // GET: /Checkout/

        public IActionResult AddressAndPayment()
        {
            return View();
        }

        //
        // POST: /Checkout/AddressAndPayment

        //Bug: Http verbs not available. Also binding to FormCollection is not available.
        //[HttpPost]
        //public IActionResult AddressAndPayment(FormCollection values)
        public IActionResult AddressAndPayment(int workaroundId)
        {
            var coll = this.Context.Request.GetFormAsync().Result;

            var order = new Order();
            //TryUpdateModel(order);

            try
            {
                //if (string.Equals(values["PromoCode"], PromoCode,
                //    StringComparison.OrdinalIgnoreCase) == false)
                if (string.Equals(coll.GetValues("PromoCode").FirstOrDefault(), PromoCode,
                    StringComparison.OrdinalIgnoreCase) == false)
                {
                    return View(order);
                }
                else
                {
                    //Bug: Identity not available
                    order.Username = null; //User.Identity.Name;
                    order.OrderDate = DateTime.Now;

                    //Add the Order
                    storeDB.Orders.Add(order);

                    //Process the order
                    var cart = ShoppingCart.GetCart(storeDB, this.Context);
                    cart.CreateOrder(order);

                    // Save all changes
                    storeDB.SaveChanges();

                    //Bug: Helper not available
                    //return RedirectToAction("Complete",
                    //    new { id = order.OrderId });
                    return View();
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
            //Bug: Identity not available
            //bool isValid = storeDB.Orders.Any(
            //    o => o.OrderId == id &&
            //    o.Username == User.Identity.Name);

            bool isValid = storeDB.Orders.Any(
                o => o.OrderId == id);

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
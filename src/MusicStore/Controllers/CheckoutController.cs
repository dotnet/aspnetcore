// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    //Bug: Missing auth filter
    //[Authorize]
    public class CheckoutController : Controller
    {
        MusicStoreContext db = new MusicStoreContext();
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
        public async Task<IActionResult> AddressAndPayment(int workaroundId)
        {
            var formCollection = await Context.Request.GetFormAsync();

            var order = new Order();
            //TryUpdateModel(order);

            try
            {
                //if (string.Equals(values["PromoCode"], PromoCode,
                //    StringComparison.OrdinalIgnoreCase) == false)
                if (string.Equals(formCollection.GetValues("PromoCode").FirstOrDefault(), PromoCode,
                    StringComparison.OrdinalIgnoreCase) == false)
                {
                    return View(order);
                }
                else
                {
                    // TODO [EF] Swap to store generated identity key when supported
                    var nextId = db.Orders.Any()
                        ? db.Orders.Max(o => o.OrderId) + 1
                        : 1;

                    //Bug: Object values should come from page (putting in hard coded values as EF can't work with nulls against SQL Server yet)
                    //Bug: Identity not available
                    order.Username = "unknown"; //User.Identity.Name;
                    order.OrderId = nextId;
                    order.OrderDate = DateTime.Now;
                    order.FirstName = "John";
                    order.LastName = "Doe";
                    order.Address = "One Microsoft Way";
                    order.City = "Redmond";
                    order.State = "WA";
                    order.Country = "USA";
                    order.Email = "john.doe@example.com";
                    order.Phone = "555-555-5555";
                    order.PostalCode = "98052";

                    //Add the Order
                    db.Orders.Add(order);

                    //Process the order
                    var cart = ShoppingCart.GetCart(db, this.Context);
                    cart.CreateOrder(order);

                    // Save all changes
                    db.SaveChanges();

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

            bool isValid = db.Orders.Any(
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
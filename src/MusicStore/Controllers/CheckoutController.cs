using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly MusicStoreContext db;

        public CheckoutController(MusicStoreContext context)
        {
            db = context;
        }

        const string PromoCode = "FREE";

        //
        // GET: /Checkout/

        public IActionResult AddressAndPayment()
        {
            return View();
        }

        //
        // POST: /Checkout/AddressAndPayment

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddressAndPayment(Order order)
        {
            var formCollection = await Context.Request.GetFormAsync();

            try
            {
                if (string.Equals(formCollection.GetValues("PromoCode").FirstOrDefault(), PromoCode,
                    StringComparison.OrdinalIgnoreCase) == false)
                {
                    return View(order);
                }
                else
                {
                    order.Username = Context.User.Identity.GetUserName();
                    order.OrderDate = DateTime.Now;

                    //Add the Order
                    db.Orders.Add(order);

                    //Process the order
                    var cart = ShoppingCart.GetCart(db, this.Context);
                    cart.CreateOrder(order);

                    // Save all changes
                    db.SaveChanges();

                    return RedirectToAction("Complete",
                        new { id = order.OrderId });
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
            bool isValid = db.Orders.Any(
                o => o.OrderId == id &&
                o.Username == Context.User.Identity.GetUserName());

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
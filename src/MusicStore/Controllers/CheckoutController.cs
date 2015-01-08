using System;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private const string PromoCode = "FREE";
        private readonly MusicStoreContext _dbContext;

        public CheckoutController(MusicStoreContext dbContext)
        {
            _dbContext = dbContext;
        }

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
            var formCollection = await Context.Request.ReadFormAsync();

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
                    await _dbContext.Orders.AddAsync(order, Context.RequestAborted);

                    //Process the order
                    var cart = ShoppingCart.GetCart(_dbContext, Context);
                    await cart.CreateOrder(order);

                    // Save all changes
                    await _dbContext.SaveChangesAsync(Context.RequestAborted);

                    return RedirectToAction("Complete", new { id = order.OrderId });
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

        public async Task<IActionResult> Complete(int id)
        {
            // Validate customer owns this order
            bool isValid = await _dbContext.Orders.AnyAsync(
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
using Microsoft.AspNet.Mvc;
using MvcSample.Models;

namespace MvcSample
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("MyView", User());
        }

        public IActionResult Something()
        {
            return new ContentResult
            {
                Content = "Hello World From Content"
            };
        }

        public IActionResult Hello()
        {
            return Result.Content("Hello World");
        }

        public void Raw()
        {
            Context.Response.WriteAsync("Hello World raw");
        }

        public User User()
        {
            User user = new User()
            {
                Name = "My name",
                Address = "My address"
            };

            return user;
        }

        public IActionResult MyView()
        {
            return View(User());
        }
    }
}
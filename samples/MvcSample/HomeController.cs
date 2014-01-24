using Microsoft.AspNet.Mvc;
using Microsoft.Owin;

namespace MvcSample
{
    public class HomeController : Controller
    {
        public string Index()
        {
            return "Hello World";
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
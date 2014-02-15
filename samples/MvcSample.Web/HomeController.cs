using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class HomeController : Controller
    {
        public string Index()
        {
            return "Hello from the new MVC";
        }

        public IActionResult Test()
        {
            return View();
        }
    }
}

using Microsoft.AspNet.Mvc;

namespace MvcSample
{
    public class LinkController : Controller
    {
        public IActionResult Details()
        {
            return View();
        }

        public string About()
        {
            return Url.Action(null);
        }

        public string Get()
        {
            return Url.Route(new { controller = "Home", action = "Details" });
        }
    }
}

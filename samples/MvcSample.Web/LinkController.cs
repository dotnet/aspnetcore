
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace MvcSample.Web
{
    public class LinkController : Controller
    {
        public ActionResult Details()
        {
            return View();
        }

        public string About()
        {
            return Url.Action(null);
        }

        public string Get()
        {
            // Creates a url like: http://localhost:58195/Home/Details#CoolBeans!
            return Url.RouteUrl("CoolBeansRoute", new { controller = "Home", action = "Details" }, protocol: "http", host: null, fragment: "CoolBeans!");
        }

        public string Link1()
        {
            return Url.Action("Index", "Home");
        }

        public string Link2()
        {
            return Url.Action("Link2");
        }
    }
}

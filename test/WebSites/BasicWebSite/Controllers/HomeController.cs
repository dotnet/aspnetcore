using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
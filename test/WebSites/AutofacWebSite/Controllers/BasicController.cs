using Microsoft.AspNet.Mvc;

namespace AutofacWebSite.Controllers
{
    public class BasicController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Certificate.Sample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

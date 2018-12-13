using Microsoft.AspNetCore.Mvc;

namespace IdentitySample.DefaultUI.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
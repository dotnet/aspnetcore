using Microsoft.AspNetCore.Mvc;

namespace SimpleAppDesktopOnly.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}

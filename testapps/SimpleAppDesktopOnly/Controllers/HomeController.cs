using Microsoft.AspNetCore.Mvc;

namespace SimpleApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}

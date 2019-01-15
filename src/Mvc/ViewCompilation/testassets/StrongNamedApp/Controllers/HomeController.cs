using Microsoft.AspNetCore.Mvc;

namespace StrongNamedApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace SimpleAppWithAssemblyRename.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

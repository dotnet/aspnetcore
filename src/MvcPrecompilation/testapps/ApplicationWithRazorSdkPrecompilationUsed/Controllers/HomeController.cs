using Microsoft.AspNetCore.Mvc;

namespace ApplicationWithRazorSdkPrecompilationUsed.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("Views/Home/Index.cshtml");
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }
    }
}

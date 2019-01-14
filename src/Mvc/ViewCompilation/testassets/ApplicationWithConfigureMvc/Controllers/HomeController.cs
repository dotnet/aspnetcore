using Microsoft.AspNetCore.Mvc;

namespace ApplicationWithConfigureStartup.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult ViewWithPreprocessor() => View();
    }
}

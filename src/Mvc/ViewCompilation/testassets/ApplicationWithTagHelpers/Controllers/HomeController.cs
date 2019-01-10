using Microsoft.AspNetCore.Mvc;

namespace ApplicationWithTagHelpers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult ClassLibraryTagHelper() => View();

        public IActionResult LocalTagHelper() => View();

        public IActionResult About() => Content("About content");
    }
}

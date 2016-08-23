using Microsoft.AspNetCore.Mvc;

namespace ClassLibraryWithPrecompiledViews
{
    [Area("Manage")]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}

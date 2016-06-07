using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NodeServicesExamples.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(int pageIndex)
        {
            return View();
        }

        public IActionResult ES2015Transpilation()
        {
            return View();
        }

        public IActionResult ImageResizing()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}

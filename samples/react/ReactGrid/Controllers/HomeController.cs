using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ReactExample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(int pageIndex)
        {
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}


using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Areas.Travel.Controllers
{
    [Area("Travel")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Result.Content("This is the Travel/Home/Index action.");
        }
    }
}
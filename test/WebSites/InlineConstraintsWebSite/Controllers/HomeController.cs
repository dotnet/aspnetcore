using Microsoft.AspNet.Mvc;

namespace InlineConstraints.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }        
    }
}
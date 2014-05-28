using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }        

        public IActionResult PlainView()
        {
            return View();
        }

        public IActionResult NoContentResult()
        {
            return new HttpStatusCodeResult(204);
        }
    }
}
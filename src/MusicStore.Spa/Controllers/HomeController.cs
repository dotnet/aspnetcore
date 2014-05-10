using Microsoft.AspNet.Mvc;

namespace MusicStore.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public IActionResult Index()
        {
            return View();
        }
    }
}
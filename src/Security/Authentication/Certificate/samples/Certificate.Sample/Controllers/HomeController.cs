using Microsoft.AspNetCore.Mvc;

namespace idunno.Authentication.Certificate.Sample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

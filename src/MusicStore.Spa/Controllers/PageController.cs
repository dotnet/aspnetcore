using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace MusicStore.Spa.Controllers
{
    [Route("/")]
    public class PageController : Controller
    {
        [HttpGet]
        public IActionResult Home()
        {
            return View("/Pages/Home.cshtml");
        }

        [HttpGet("admin")]
        [Authorize("app-ManageStore")]
        public IActionResult Admin()
        {
            return View("/Pages/Admin.cshtml");
        }
    }
}

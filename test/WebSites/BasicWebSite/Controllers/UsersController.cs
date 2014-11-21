using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return Content("Users.Index");
        }
    }
}
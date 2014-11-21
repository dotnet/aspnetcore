using Microsoft.AspNet.Mvc;

namespace InlineConstraints.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return Content("Users.Index");
        }
    }
}
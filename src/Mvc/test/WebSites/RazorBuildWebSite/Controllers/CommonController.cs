
using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite.Controllers
{
    public class CommonController : Controller
    {
        public new ActionResult View()
        {
            return base.View("CommonView");
        }
    }
}

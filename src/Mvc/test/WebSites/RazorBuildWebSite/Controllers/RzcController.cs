
using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite.Controllers
{
    public class RzcController : Controller
    {
        public new ActionResult View()
        {
            return base.View();
        }
    }
}


using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite.Controllers
{
    public class PrecompilationController : Controller
    {
        public new ActionResult View()
        {
            return base.View();
        }
    }
}

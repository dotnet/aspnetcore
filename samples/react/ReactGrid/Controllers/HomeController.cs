using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.NodeServices.React;

namespace ReactExample.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(int pageIndex)
        {
            ViewData["ReactOutput"] = await ReactRenderer.RenderToString(
                moduleName: "ReactApp/components/ReactApp.jsx",
                exportName: "ReactApp",
                baseUrl: Request.Path
            );
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}

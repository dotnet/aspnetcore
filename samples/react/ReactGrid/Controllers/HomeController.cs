using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.NodeServices.React;

namespace ReactExample.Controllers
{
    public class HomeController : Controller
    {
        private INodeServices nodeServices;

        public HomeController(INodeServices nodeServices) {
            this.nodeServices = nodeServices;
        }
        
        public async Task<IActionResult> Index(int pageIndex)
        {
            ViewData["ReactOutput"] = await ReactRenderer.RenderToString(this.nodeServices,
                moduleName: "ReactApp/components/ReactApp.jsx",
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

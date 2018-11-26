using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;

namespace NodeServicesExamples.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(int pageIndex)
        {
            return View();
        }

        public IActionResult ES2015Transpilation()
        {
            return View();
        }

        public async Task<IActionResult> Chart([FromServices] INodeServices nodeServices)
        {
            var options = new { width = 400, height = 200, showArea = true, showPoint = true, fullWidth = true };
            var data = new
            {
                labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" },
                series = new[] {
                    new[] { 1, 5, 2, 5, 4, 3 },
                    new[] { 2, 3, 4, 8, 1, 2 },
                    new[] { 5, 4, 3, 2, 1, 0 }
                }
            };

            ViewData["ChartMarkup"] = await nodeServices.InvokeAsync<string>("./Node/renderChart", "line", options, data);

            return View();
        }

        public async Task<IActionResult> Prerendering([FromServices] ISpaPrerenderer prerenderer)
        {
            var result = await prerenderer.RenderToString("./Node/prerenderPage");

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            ViewData["PrerenderedHtml"] = result.Html;
            ViewData["PrerenderedGlobals"] = result.CreateGlobalsAssignmentScript();
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}

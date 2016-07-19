using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Webpack.ActionResults;

namespace Webpack.Controllers
{
    // This sample shows how you could invoke the prerendering APIs directly from an MVC
    // action result.
    public class FullPagePrerenderingController : Controller
    {
        private static JavaScriptModuleExport BootModule = new JavaScriptModuleExport("Clientside/PrerenderingSample")
        {
            // Because the boot module is written in TypeScript, we need to specify a webpack
            // config so it can be built. If it was written in JavaScript, this would not be needed.
            WebpackConfig = "webpack.config.js"
        };

        public IActionResult Index()
        {
            var dataToSupply = new { nowTime = DateTime.Now.Ticks };
            return this.Prerender(BootModule, dataToSupply);
        }
    }
}

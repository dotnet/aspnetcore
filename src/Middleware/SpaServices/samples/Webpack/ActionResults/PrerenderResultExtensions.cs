using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.Prerendering;

namespace Webpack.ActionResults
{
    public static class PrerenderResultExtensions
    {
        public static PrerenderResult Prerender(this ControllerBase controller, JavaScriptModuleExport exportToPrerender, object dataToSupply = null)
        {
            return new PrerenderResult(exportToPrerender, dataToSupply);
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection;

namespace Webpack.ActionResults
{
    // This is an example of how you could invoke the prerendering API from an ActionResult, so as to
    // prerender a SPA component as the entire response page (instead of injecting the SPA component
    // into a Razor view's output)
    public class PrerenderResult : ActionResult
    {
        private JavaScriptModuleExport _moduleExport;
        private object _dataToSupply;

        public PrerenderResult(JavaScriptModuleExport moduleExport, object dataToSupply = null)
        {
            _moduleExport = moduleExport;
            _dataToSupply = dataToSupply;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var nodeServices = context.HttpContext.RequestServices.GetRequiredService<INodeServices>();
            var hostEnv = context.HttpContext.RequestServices.GetRequiredService<IHostingEnvironment>();
            var applicationLifetime = context.HttpContext.RequestServices.GetRequiredService<IApplicationLifetime>();
            var applicationBasePath = hostEnv.ContentRootPath;
            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            var prerenderedHtml = await Prerenderer.RenderToString(
                applicationBasePath,
                nodeServices,
                applicationLifetime.ApplicationStopping,
                _moduleExport,
                request.GetEncodedUrl(),
                request.Path + request.QueryString.Value,
                _dataToSupply,
                /* timeoutMilliseconds */ 30000,
                /* requestPathBase */ "/"
            );

            response.ContentType = "text/html";
            await response.WriteAsync(prerenderedHtml.Html);
        }
    }
}
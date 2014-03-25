using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewResult : IActionResult
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewEngine _viewEngine;

        public ViewResult(IServiceProvider serviceProvider, IViewEngine viewEngine)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
        }

        public string ViewName {get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var view = await FindView(context.RouteValues, viewName);

            using (view as IDisposable)
            {
                context.HttpContext.Response.ContentType = "text/html";
                using (var writer = new StreamWriter(context.HttpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                {
                    var viewContext = CreateViewContext(context, writer);
                    await view.RenderAsync(viewContext);
                }
            }
        }

        private async Task<IView> FindView([NotNull] IDictionary<string, object> context,[NotNull] string viewName)
        {
            var result = await _viewEngine.FindView(context, viewName);
            result.EnsureSuccess();
            return result.View;
        }

        private ViewContext CreateViewContext([NotNull] ActionContext actionContext, [NotNull] TextWriter writer)
        {
            var urlHelper = new UrlHelper(actionContext.HttpContext, actionContext.Router, actionContext.RouteValues);

            var viewContext = new ViewContext(_serviceProvider, actionContext.HttpContext, actionContext.RouteValues)
            {
                Url = urlHelper,
                ViewData = ViewData,
                Writer = writer,
            };

            viewContext.Component = new DefaultViewComponentHelper(
                _serviceProvider.GetService<IViewComponentSelector>(),
                _serviceProvider.GetService<IViewComponentInvokerFactory>(),
                viewContext);

            return viewContext;
        }
    }
}

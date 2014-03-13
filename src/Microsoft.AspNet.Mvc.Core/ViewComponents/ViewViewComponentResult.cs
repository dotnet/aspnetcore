
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewViewComponentResult : IViewComponentResult
    {
        // {0} is the component name, {1} is the view name.
        private const string ViewPathFormat = "Components/{0}/{1}";

        private readonly IViewEngine _viewEngine;
        private readonly string _viewName;
        private readonly ViewData _viewData;

        public ViewViewComponentResult([NotNull] IViewEngine viewEngine, [NotNull] string viewName, ViewData viewData)
        {
            _viewEngine = viewEngine;
            _viewName = viewName;
            _viewData = viewData;
        }

        public void Execute([NotNull] ViewComponentContext context)
        {
            throw new NotImplementedException("There's no support for syncronous views right now.");
        }

        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            var childViewContext = new ViewContext(
                context.ViewContext.ServiceProvider,
                context.ViewContext.HttpContext,
                context.ViewContext.ViewEngineContext)
            {
                Component = context.ViewContext.Component,
                Url = context.ViewContext.Url,
                ViewData = _viewData ?? context.ViewContext.ViewData,
                Writer = context.Writer,
            };

            string qualifiedViewName;
            if (_viewName.Length > 0 && _viewName[0] == '/')
            {
                // View name that was passed in is already a rooted path, the view engine will handle this.
                qualifiedViewName = _viewName;
            }
            else
            {
                // This will produce a string like: 
                //  
                //  Components/Cart/Default
                //
                // The view engine will combine this with other path info to search paths like:
                //
                //  Views/Shared/Components/Cart/Default.cshtml
                //  Views/Home/Components/Cart/Default.cshtml
                //  Areas/Blog/Views/Shared/Components/Cart/Default.cshtml
                //
                // This supports a controller or area providing an override for component views.
                qualifiedViewName = string.Format(
                    CultureInfo.InvariantCulture,
                    ViewPathFormat,
                    ViewComponentConventions.GetComponentName(context.ComponentType),
                    _viewName);
            }

            var view = await FindView(context.ViewContext.ViewEngineContext, qualifiedViewName);
            using (view as IDisposable)
            {
                await view.RenderAsync(childViewContext, context.Writer);
            }
        }

        private async Task<IView> FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName)
        {
            // Issue #161 in Jira tracks unduping this code.
            var result = await _viewEngine.FindView(context, viewName);
            if (!result.Success)
            {
                var locationsText = string.Join(Environment.NewLine, result.SearchedLocations);
                const string message = @"The view &apos;{0}&apos; was not found. The following locations were searched:{1}.";
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    message,
                    viewName,
                    locationsText));
            }

            return result.View;
        }
    }
}

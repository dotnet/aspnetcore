using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngine : IViewEngine
    {
        private static readonly string[] _viewLocationFormats =
        {
            "/Areas/{2}/Views/{1}/{0}.cshtml",
            "/Areas/{2}/Views/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml",
        };

        private readonly IVirtualPathViewFactory _virtualPathFactory;

        public RazorViewEngine(IVirtualPathViewFactory virtualPathFactory)
        {
            _virtualPathFactory = virtualPathFactory;
        }

        public IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        public async Task<ViewEngineResult> FindView(object context, string viewName)
        {
            var actionContext = (ActionContext)context;

            var actionDescriptor = actionContext.ActionDescriptor;
            
            if (actionDescriptor == null)
            {
                return null;
            }
            
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = actionDescriptor.Name;
            }

            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "viewName");
            }

            var nameRepresentsPath = IsSpecificPath(viewName);

            if (nameRepresentsPath)
            {
                var view = await _virtualPathFactory.CreateInstance(viewName);
                return view != null ? ViewEngineResult.Found(view) :
                                      ViewEngineResult.NotFound(new[] { viewName });
            }
            else
            {
                var controllerName = actionContext.RouteValues.GetValueOrDefault<string>("controller");
                var areaName = actionContext.RouteValues.GetValueOrDefault<string>("area");

                var searchedLocations = new List<string>(_viewLocationFormats.Length);
                for (int i = 0; i < _viewLocationFormats.Length; i++)
                {
                    var path = String.Format(CultureInfo.InvariantCulture, _viewLocationFormats[i], viewName, controllerName, areaName);
                    IView view = await _virtualPathFactory.CreateInstance(path);
                    if (view != null)
                    {
                        return ViewEngineResult.Found(view);
                    }
                    searchedLocations.Add(path);
                }
                return ViewEngineResult.NotFound(searchedLocations);
            }
        }

        private static bool IsSpecificPath(string name)
        {
            char c = name[0];
            return (name[0] == '/');
        }
    }
}

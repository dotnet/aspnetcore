using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngine : IViewEngine
    {
        private static readonly string[] _viewLocationFormats = new[]
        {
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml",
        };
        private readonly IRouteContextProvider _actionDescriptorProvider;
        private readonly IVirtualPathViewFactory _virtualPathFactory;

        public RazorViewEngine(IRouteContextProvider actionDescriptorProvider, 
                               IVirtualPathViewFactory virtualPathFactory)
        {
            _actionDescriptorProvider = actionDescriptorProvider;
            _virtualPathFactory = virtualPathFactory;
        }

        public IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        public async Task<ViewEngineResult> FindView(RequestContext requestContext, string viewName)
        {
            var actionDescriptor = _actionDescriptorProvider.CreateDescriptor(requestContext) as ControllerActionRouteContext;
            
            if (actionDescriptor == null)
            {
                return null;
            }
            
            if (String.IsNullOrEmpty(viewName))
            {
                viewName = actionDescriptor.ActionName;
            }

            if (String.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "viewName");
            }

            bool nameRepresentsPath = IsSpecificPath(viewName);

            if (nameRepresentsPath)
            {
                IView view = await _virtualPathFactory.CreateInstance(viewName);
                return view != null ? ViewEngineResult.Found(view) :
                                      ViewEngineResult.NotFound(new[] { viewName });
            }
            else
            {
                string controllerName = actionDescriptor.ControllerName;
                var searchedLocations = new List<string>(_viewLocationFormats.Length);
                for (int i = 0; i < _viewLocationFormats.Length; i++)
                {
                    string path = String.Format(CultureInfo.InvariantCulture, _viewLocationFormats[i], viewName, controllerName);
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

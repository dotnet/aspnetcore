using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngine : IViewEngine
    {
        private static readonly string[] _viewLocationFormats =
        {
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml",
        };

        private readonly IActionDescriptorProvider _actionDescriptorProvider;
        private readonly IVirtualPathViewFactory _virtualPathFactory;

        public RazorViewEngine(IActionDescriptorProvider actionDescriptorProvider, 
                               IVirtualPathViewFactory virtualPathFactory)
        {
            _actionDescriptorProvider = actionDescriptorProvider;
            _virtualPathFactory = virtualPathFactory;
        }

        public IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        public async Task<ViewEngineResult> FindView(object context, string viewName)
        {
            var actionContext = (ActionContext)context;

            // TODO: We plan to change this on the next CR, so we don't have a strong depenedency directly on the specific
            // type of the action descriptor
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
                var controllerName = actionDescriptor.Path;
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

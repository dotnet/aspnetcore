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
            "~/Views/{1}/{0}.cshtml",
            "~/Views/Shared/{0}.cshtml",
        };
        private readonly IActionDescriptorProvider _actionDescriptorProvider;
        private readonly IVirtualPathFactory _virtualPathFactory;

        public RazorViewEngine(IActionDescriptorProvider actionDescriptorProvider, 
                               IVirtualPathFactory virtualPathFactory)
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
            var actionDescriptor = _actionDescriptorProvider.CreateDescriptor(requestContext) as ControllerBasedActionDescriptor;
            
            if (actionDescriptor == null)
            {
                return null;
            }
            
            if (String.IsNullOrEmpty(viewName))
            {
                viewName = actionDescriptor.ActionName;
            }

            string controllerName = actionDescriptor.ControllerName;
            var searchedLocations = new List<string>(_viewLocationFormats.Length);
            for (int i = 0; i < _viewLocationFormats.Length; i++)
            {
                string path = String.Format(CultureInfo.InvariantCulture, _viewLocationFormats[i], viewName, controllerName);
                RazorView view = (RazorView)(await _virtualPathFactory.CreateInstance(path));
                if (view != null)
                {
                    return ViewEngineResult.Found(view);
                }
                searchedLocations.Add(path);
            }
            return ViewEngineResult.NotFound(searchedLocations);
        }
    }
}

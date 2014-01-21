using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.View
{
    public class CompositeViewEngine : IViewEngine
    {
        private readonly List<IViewEngine> _viewEngines;

        public CompositeViewEngine()
            : this(Enumerable.Empty<IViewEngine>())
        {
        }

        public CompositeViewEngine(IEnumerable<IViewEngine> viewEngines)
        {
            _viewEngines = viewEngines.ToList();
        }

        public void Insert(int index, IViewEngine viewEngine)
        {
            _viewEngines.Insert(index, viewEngine);
        }

        public void Add(IViewEngine viewEngine)
        {
            _viewEngines.Add(viewEngine);
        }

        public async Task<ViewEngineResult> FindView(RequestContext controllerContext, string viewName)
        {
            if (_viewEngines.Count == 0)
            {
                return ViewEngineResult.NotFound(Enumerable.Empty<string>());
            }

            var searchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _viewEngines.Count; i++)
            {
                ViewEngineResult result = await _viewEngines[i].FindView(controllerContext, viewName);
                if (result.Success)
                {
                    return result;
                }
                foreach (string location in result.SearchedLocations)
                {
                    searchedPaths.Add(location);
                }
            }

            return ViewEngineResult.NotFound(searchedPaths);
        }
    }
}

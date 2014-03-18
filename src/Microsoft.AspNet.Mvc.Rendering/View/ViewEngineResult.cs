using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewEngineResult
    {
        private ViewEngineResult()
        {
        }

        public IEnumerable<string> SearchedLocations { get; private set; }

        public IView View { get; private set; }

        public bool Success 
        { 
            get { return View != null; } 
        }

        public static ViewEngineResult NotFound([NotNull] IEnumerable<string> searchedLocations)
        {
            return new ViewEngineResult
            {
                SearchedLocations = searchedLocations
            };
        }

        public static ViewEngineResult Found([NotNull] IView view)
        {
            return new ViewEngineResult
            {
                View = view
            };
        }
    }
}

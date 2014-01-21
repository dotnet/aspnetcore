using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
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

        public static ViewEngineResult NotFound(IEnumerable<string> searchedLocations)
        {
            if (searchedLocations == null)
            {
                throw new ArgumentNullException("searchedLocations");
            }

            return new ViewEngineResult
            {
                SearchedLocations = searchedLocations
            };
        }

        public static ViewEngineResult Found(IView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            return new ViewEngineResult
            {
                View = view
            };
        }
    }
}

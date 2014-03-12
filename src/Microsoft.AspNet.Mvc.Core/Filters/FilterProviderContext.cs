using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterProviderContext
    {
        public FilterProviderContext(ActionDescriptor actionDescriptor)
        {
            ActionDescriptor = actionDescriptor;
        }

        // Input
        public ActionDescriptor ActionDescriptor { get; set; }

        // Result
        public List<IFilter> OrderedFilterList { get; set; }
    }
}

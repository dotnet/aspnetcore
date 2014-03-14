using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterProviderContext
    {
        public FilterProviderContext(ActionDescriptor actionDescriptor, List<FilterItem> items)
        {
            ActionDescriptor = actionDescriptor;
            Result = items;
        }

        // Input
        public ActionDescriptor ActionDescriptor { get; set; }

        // Result
        public List<FilterItem> Result { get; set; }
    }
}

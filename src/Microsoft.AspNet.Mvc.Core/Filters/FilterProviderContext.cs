using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterProviderContext
    {
        public FilterProviderContext(ActionDescriptor actionDescriptor, List<FilterItem> items)
        {
            ActionDescriptor = actionDescriptor;
            Items = items;
        }

        // Input
        public ActionDescriptor ActionDescriptor { get; set; }

        // Result
        public List<FilterItem> Items { get; set; }

        public class FilterItem
        {
            public FilterItem([NotNull] FilterDescriptor descriptor)
            {
                Descriptor = descriptor;
            }

            public FilterItem([NotNull] FilterDescriptor descriptor, [NotNull] IFilter filter) : this(descriptor)
            {
                Filter = filter;
            }

            public FilterDescriptor Descriptor { get; set; }

            public IFilter Filter { get; set; }
        }
    }
}

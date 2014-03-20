using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class FilterContext
    {
        public FilterContext([NotNull] ActionContext actionContext, [NotNull] IReadOnlyList<FilterItem> filterItems)
        {
            ActionContext = actionContext;
            FilterItems = filterItems;
        }

        public ActionContext ActionContext { get; private set; }
        public IReadOnlyList<FilterItem> FilterItems { get; private set; }

        // Result
        public virtual IActionResult ActionResult { get; set; }
    }
}

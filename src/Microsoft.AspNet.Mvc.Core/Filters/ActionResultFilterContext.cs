using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFilterContext : FilterContext
    {
        public ActionResultFilterContext(ActionContext actionContext, IReadOnlyList<FilterItem> filterItems, IActionResult initialResult)
            : base(actionContext, filterItems)
        {
            ActionResult = initialResult;
        }
    }
}

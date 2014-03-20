using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public class ActionFilterContext : FilterContext
    {
        public ActionFilterContext([NotNull] ActionContext actionContext,
                                   [NotNull] IReadOnlyList<FilterItem> filterItems,
                                   [NotNull] IDictionary<string, object> actionArguments)
            : base(actionContext, filterItems)
        {
            ActionArguments = actionArguments;
        }

        public virtual IDictionary<string, object> ActionArguments { get; private set; }
    }
}

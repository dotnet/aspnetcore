using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionExecutingContext : FilterContext
    {
        public ActionExecutingContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters,
            [NotNull] IDictionary<string, object> actionArguments)
            : base(actionContext, filters)
        {
            ActionArguments = actionArguments;
        }

        public virtual IActionResult Result { get; set; }

        public virtual IDictionary<string, object> ActionArguments { get; private set; }
    }
}

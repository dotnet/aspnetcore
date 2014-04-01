using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public abstract class FilterContext : ActionContext
    {
        public FilterContext(
            [NotNull] ActionContext actionContext, 
            [NotNull] IList<IFilter> filters) 
            : base(actionContext)
        {
            Filters = filters;
        }

        public virtual IList<IFilter> Filters { get; private set; }
    }
}

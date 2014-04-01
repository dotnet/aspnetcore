using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ResultExecutingContext : FilterContext
    {
        public ResultExecutingContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters,
            [NotNull] IActionResult result)
            : base(actionContext, filters)
        {
            Result = result;
        }

        public virtual IActionResult Result { get; set; }

        public virtual bool Cancel { get; set; }
    }
}

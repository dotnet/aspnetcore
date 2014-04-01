using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public class AuthorizationContext : FilterContext
    {
        public AuthorizationContext(
            [NotNull] ActionContext actionContext, 
            [NotNull] IList<IFilter> filters)
            : base(actionContext, filters)
        {
        }

        public virtual IActionResult Result { get; set; }
    }
}

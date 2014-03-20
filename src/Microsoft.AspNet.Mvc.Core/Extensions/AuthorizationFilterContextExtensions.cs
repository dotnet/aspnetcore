using System.Linq;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public static class AuthorizationFilterContextExtensions
    {
        public static bool HasAllowAnonymous([NotNull] this AuthorizationFilterContext context)
        {
            return context.FilterItems.Any(item => item.Filter is IAllowAnonymous);
        }
    }
}

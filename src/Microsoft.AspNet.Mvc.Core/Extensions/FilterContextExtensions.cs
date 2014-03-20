using System.Linq;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public static class FilterContextExtensions
    {
        public static bool HasAllowAnonymous([NotNull] this FilterContext context)
        {
            return context.FilterItems.Any(item => item.Filter is IAllowAnonymous);
        }
    }
}

using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public interface IAuthorizationFilter : IFilter<AuthorizationFilterContext>
    {
    }
}

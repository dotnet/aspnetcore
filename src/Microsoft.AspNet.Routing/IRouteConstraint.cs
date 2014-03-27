using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteConstraint
    {
        bool Match([NotNull] HttpContext httpContext,
                   [NotNull] IRouter route,
                   [NotNull] string routeKey,
                   [NotNull] IDictionary<string, object> values,
                   RouteDirection routeDirection);
    }
}

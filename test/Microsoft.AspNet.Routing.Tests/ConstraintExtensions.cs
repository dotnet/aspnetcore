using Microsoft.AspNet.Abstractions;
using Moq;

namespace Microsoft.AspNet.Routing.Tests
{
    public static class RouteConstraintExtensions
    {
        public static bool EasyMatch(this IRouteConstraint constraint,
                                     string routeKey,
                                     RouteValueDictionary values)
        {
            return constraint.Match(httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeKey: routeKey,
                values: values,
                routeDirection: RouteDirection.IncomingRequest);
        }
    }
}

using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public interface IRouteContextProvider
    {
        RouteContext CreateDescriptor(RequestContext requestContext);
    }
}

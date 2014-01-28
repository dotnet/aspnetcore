
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public interface IRoute
    {
        BoundRoute Bind(RouteBindingContext context);

        RouteMatch GetMatch(RoutingContext context);
    }
}

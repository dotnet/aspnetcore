
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteEngine
    {
        RouteMatch GetMatch(IDictionary<string, object> context);

        BoundRoute GetUrl(string routeName, IDictionary<string, object> context, IDictionary<string, object> values);
    }
}

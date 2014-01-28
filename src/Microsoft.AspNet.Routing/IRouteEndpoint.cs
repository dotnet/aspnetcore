using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteEndpoint
    {
        Func<IDictionary<string, object>, Task> AppFunc
        {
            get;
        }

        IRouteEndpoint AddRoute(string name, IRoute route);
    }
}

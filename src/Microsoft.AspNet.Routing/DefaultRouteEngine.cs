// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    internal class DefaultRouteEngine : IRouteEngine
    {
        public DefaultRouteEngine(IRouteCollection routes)
        {
            Routes = routes;
        }

        public IRouteCollection Routes
        {
            get;
            private set;
        }

        public async Task<bool> Invoke(IDictionary<string, object> context)
        {
            RouteContext routeContext = new RouteContext(context);

            for (int i = 0; i < Routes.Count; i++)
            {
                IRoute route = Routes[i];

                RouteMatch match = route.Match(routeContext);
                if (match != null)
                {
                    await match.Endpoint.Invoke(context);
                    return true;
                }
            }

            return false;
        }
    }
}

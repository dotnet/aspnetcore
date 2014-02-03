// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public class DefaultRouteEngine : IRouteEngine
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

        public async Task<bool> Invoke(HttpContext context)
        {
            RouteContext routeContext = new RouteContext(context);

            for (int i = 0; i < Routes.Count; i++)
            {
                IRoute route = Routes[i];

                RouteMatch match = route.Match(routeContext);
                if (match != null)
                {
                    context.SetFeature<IRouteValues>(new RouteValues(match.Values));

                    var accepted = await match.Endpoint.Send(context);
                    if (accepted)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

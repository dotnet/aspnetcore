// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
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
            var routeContext = new RouteContext(context);

            for (var i = 0; i < Routes.Count; i++)
            {
                var route = Routes[i];

                var match = route.Match(routeContext);
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

        public string GetUrl(HttpContext context, IDictionary<string, object> values)
        {
            var routeBindContext = new RouteBindContext(context, values);

            for (var i = 0; i < Routes.Count; i++)
            {
                var route = Routes[i];

                var result = route.Bind(routeBindContext);
                if (result != null)
                {
                    return result.Url;
                }
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRAppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HubRouteBuilder> configure)
        {
            // REVIEW: Should we discover hubs?
            app.UseSockets(routes =>
            {
                configure(new HubRouteBuilder(routes));
            });

            return app;
        }
    }

    public class HubRouteBuilder
    {
        private readonly SocketRouteBuilder _routes;

        public HubRouteBuilder(SocketRouteBuilder routes)
        {
            _routes = routes;
        }

        public void MapHub<THub>(string path) where THub : Hub
        {
            _routes.MapSocketEndpoint<HubEndPoint<THub>>(path);
        }
    }
}

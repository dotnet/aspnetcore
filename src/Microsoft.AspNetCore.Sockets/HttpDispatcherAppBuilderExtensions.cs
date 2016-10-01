using System;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpDispatcherAppBuilderExtensions
    {
        public static IApplicationBuilder UseSockets(this IApplicationBuilder app, Action<HttpConnectionDispatcher> callback)
        {
            var dispatcher = new HttpConnectionDispatcher(app);
            callback(dispatcher);

            // TODO: Use new low allocating websocket API
            app.UseWebSockets();
            app.UseRouter(dispatcher.GetRouter());
            return app;
        }
    }
}

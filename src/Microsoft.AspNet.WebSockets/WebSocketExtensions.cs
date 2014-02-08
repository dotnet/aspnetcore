#if NET45
using Microsoft.AspNet.WebSockets;

namespace Owin
{
    public static class WebSocketExtensions
    {
        public static IAppBuilder UseWebSockets(this IAppBuilder app)
        {
            return app.Use<WebSocketMiddleware>();
        }
    }
}
#endif
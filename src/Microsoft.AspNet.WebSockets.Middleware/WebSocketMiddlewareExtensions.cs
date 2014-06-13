using Microsoft.AspNet.WebSockets.Middleware;

namespace Microsoft.AspNet.Builder
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IBuilder UseWebSockets(this IBuilder builder)
        {
            return builder.Use(next => new WebSocketMiddleware(next).Invoke);
        }
    }
}
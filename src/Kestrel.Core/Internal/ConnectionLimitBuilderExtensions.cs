using Microsoft.AspNetCore.Protocols;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public static class ConnectionLimitBuilderExtensions
    {
        public static IConnectionBuilder UseConnectionLimit(this IConnectionBuilder builder, ServiceContext serviceContext)
        {
            return builder.Use(next =>
            {
                var middleware = new ConnectionLimitMiddleware(next, serviceContext);
                return middleware.OnConnectionAsync;
            });
        }
    }
}

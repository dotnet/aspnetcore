using Components.TestServer;

namespace Microsoft.AspNetCore.Builder
{
    public static class InterruptibleWebSocketAppBuilderExtensions
    {
        public static IApplicationBuilder UseInterruptibleWebSockets(
            this IApplicationBuilder builder,
            InterruptibleWebSocketOptions options)
        {
            builder.UseMiddleware<InterruptibleSocketMiddleware>(options);
            return builder;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Components.TestServer.Infrastructure
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

    public class InterruptibleSocketMiddleware
    {
        public InterruptibleSocketMiddleware(
            RequestDelegate next,
            InterruptibleWebSocketOptions options)
        {
            Next = next;
            Options = options;
        }

        public RequestDelegate Next { get; }
        public ConcurrentDictionary<string, InterruptibleWebSocket> Registry => Options.Registry;
        public InterruptibleWebSocketOptions Options { get; }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Equals(Options.InterruptPath) && context.Request.Query.TryGetValue(Options.WebSocketIdParameterName,out var currentIdentifier))
            {
                if (Registry.TryGetValue(currentIdentifier, out var webSocket))
                {
                    webSocket.Disable();
                    return;
                }
                else
                {
                    context.Response.StatusCode = 400;
                    return;
                }
            }

            if (context.Request.Path.Equals(Options.WebSocketPath, StringComparison.OrdinalIgnoreCase) &&
                context.Request.Cookies.TryGetValue(Options.WebSocketIdParameterName, out var identifier))
            {
                context.Features.Set<IHttpWebSocketFeature>(new InterruptibleWebSocketFeature(context, identifier, Registry));
            }

            await Next(context);
        }
    }
}

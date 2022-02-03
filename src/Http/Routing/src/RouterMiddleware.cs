// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Middleware responsible for routing.
/// </summary>
public partial class RouterMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly IRouter _router;

    /// <summary>
    /// Constructs a new <see cref="RouterMiddleware"/> instance with a given <paramref name="router"/>.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="router">The <see cref="IRouter"/> to use for routing requests.</param>
    public RouterMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IRouter router)
    {
        _next = next;
        _router = router;

        _logger = loggerFactory.CreateLogger<RouterMiddleware>();
    }

    /// <summary>
    /// Evaluates the handler associated with the <see cref="RouteContext"/>
    /// derived from <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">A <see cref="HttpContext"/> instance.</param>
    public async Task Invoke(HttpContext httpContext)
    {
        var context = new RouteContext(httpContext);
        context.RouteData.Routers.Add(_router);

        await _router.RouteAsync(context);

        if (context.Handler == null)
        {
            Log.RequestNotMatched(_logger);
            await _next.Invoke(httpContext);
        }
        else
        {
            var routingFeature = new RoutingFeature()
            {
                RouteData = context.RouteData
            };

            // Set the RouteValues on the current request, this is to keep the IRouteValuesFeature inline with the IRoutingFeature
            httpContext.Request.RouteValues = context.RouteData.Values;
            httpContext.Features.Set<IRoutingFeature>(routingFeature);

            await context.Handler(context.HttpContext);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Request did not match any routes", EventName = "RequestNotMatched")]
        public static partial void RequestNotMatched(ILogger logger);
    }
}

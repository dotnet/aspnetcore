// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed partial class EndpointMiddleware
{
    internal const string AuthorizationMiddlewareInvokedKey = "__AuthorizationMiddlewareWithEndpointInvoked";
    internal const string CorsMiddlewareInvokedKey = "__CorsMiddlewareWithEndpointInvoked";
    internal const string AntiforgeryMiddlewareWithEndpointInvokedKey = "__AntiforgeryMiddlewareWithEndpointInvoked";

    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly RouteOptions _routeOptions;

    public EndpointMiddleware(
        ILogger<EndpointMiddleware> logger,
        RequestDelegate next,
        IOptions<RouteOptions> routeOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeOptions = routeOptions?.Value ?? throw new ArgumentNullException(nameof(routeOptions));
    }

    public Task Invoke(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is not null)
        {
            // This check should be kept in sync with the one in EndpointRoutingMiddleware
            if (!_routeOptions.SuppressCheckForUnhandledSecurityMetadata)
            {
                if (endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null &&
                    !httpContext.Items.ContainsKey(AuthorizationMiddlewareInvokedKey))
                {
                    ThrowMissingAuthMiddlewareException(endpoint);
                }

                if (endpoint.Metadata.GetMetadata<ICorsMetadata>() is not null &&
                    !httpContext.Items.ContainsKey(CorsMiddlewareInvokedKey))
                {
                    ThrowMissingCorsMiddlewareException(endpoint);
                }

                if (endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: true } &&
                    !httpContext.Items.ContainsKey(AntiforgeryMiddlewareWithEndpointInvokedKey))
                {
                    ThrowMissingAntiforgeryMiddlewareException(endpoint);
                }
            }

            if (endpoint.RequestDelegate is not null)
            {
                if (!_logger.IsEnabled(LogLevel.Information))
                {
                    // Avoid the AwaitRequestTask state machine allocation if logging is disabled.
                    return endpoint.RequestDelegate(httpContext);
                }

                Log.ExecutingEndpoint(_logger, endpoint);

                try
                {
                    var requestTask = endpoint.RequestDelegate(httpContext);
                    if (!requestTask.IsCompletedSuccessfully)
                    {
                        return AwaitRequestTask(endpoint, requestTask, _logger);
                    }
                }
                catch
                {
                    Log.ExecutedEndpoint(_logger, endpoint);
                    throw;
                }

                Log.ExecutedEndpoint(_logger, endpoint);
                return Task.CompletedTask;
            }
        }

        return _next(httpContext);

        static async Task AwaitRequestTask(Endpoint endpoint, Task requestTask, ILogger logger)
        {
            try
            {
                await requestTask;
            }
            finally
            {
                Log.ExecutedEndpoint(logger, endpoint);
            }
        }
    }

    private static void ThrowMissingAuthMiddlewareException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains authorization metadata, " +
            "but a middleware was not found that supports authorization." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAuthorization() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAuthorization() must go between them.");
    }

    private static void ThrowMissingCorsMiddlewareException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains CORS metadata, " +
            "but a middleware was not found that supports CORS." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseCors() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseCors() must go between them.");
    }

    private static void ThrowMissingAntiforgeryMiddlewareException(Endpoint endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains anti-forgery metadata, " +
            "but a middleware was not found that supports anti-forgery." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAntiforgery() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAntiforgery() must go between them. " +
            "Calls to app.UseAntiforgery() must be placed after calls to app.UseAuthentication() and app.UseAuthorization().");
    }

    private static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Executing endpoint '{EndpointName}'", EventName = "ExecutingEndpoint")]
        public static partial void ExecutingEndpoint(ILogger logger, Endpoint endpointName);

        [LoggerMessage(1, LogLevel.Information, "Executed endpoint '{EndpointName}'", EventName = "ExecutedEndpoint")]
        public static partial void ExecutedEndpoint(ILogger logger, Endpoint endpointName);
    }
}

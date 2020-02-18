// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class EndpointMiddleware
    {
        internal const string AuthorizationMiddlewareInvokedKey = "__AuthorizationMiddlewareWithEndpointInvoked";
        internal const string CorsMiddlewareInvokedKey = "__CorsMiddlewareWithEndpointInvoked";

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
            if (endpoint?.RequestDelegate != null)
            {
                if (!_routeOptions.SuppressCheckForUnhandledSecurityMetadata)
                {
                    if (endpoint.Metadata.GetMetadata<IAuthorizeData>() != null &&
                        !httpContext.Items.ContainsKey(AuthorizationMiddlewareInvokedKey))
                    {
                        ThrowMissingAuthMiddlewareException(endpoint);
                    }

                    if (endpoint.Metadata.GetMetadata<ICorsMetadata>() != null &&
                        !httpContext.Items.ContainsKey(CorsMiddlewareInvokedKey))
                    {
                        ThrowMissingCorsMiddlewareException(endpoint);
                    }
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
                catch (Exception exception)
                {
                    Log.ExecutedEndpoint(_logger, endpoint);
                    return Task.FromException(exception);
                }

                Log.ExecutedEndpoint(_logger, endpoint);
                return Task.CompletedTask;
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
                "Configure your application startup by adding app.UseAuthorization() inside the call to Configure(..) in the application startup code. The call to app.UseAuthorization() must appear between app.UseRouting() and app.UseEndpoints(...).");
        }

        private static void ThrowMissingCorsMiddlewareException(Endpoint endpoint)
        {
            throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains CORS metadata, " +
                "but a middleware was not found that supports CORS." +
                Environment.NewLine +
                "Configure your application startup by adding app.UseCors() inside the call to Configure(..) in the application startup code. The call to app.UseCors() must appear between app.UseRouting() and app.UseEndpoints(...).");
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _executingEndpoint = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(0, "ExecutingEndpoint"),
                "Executing endpoint '{EndpointName}'");

            private static readonly Action<ILogger, string, Exception> _executedEndpoint = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, "ExecutedEndpoint"),
                "Executed endpoint '{EndpointName}'");

            public static void ExecutingEndpoint(ILogger logger, Endpoint endpoint)
            {
                _executingEndpoint(logger, endpoint.DisplayName, null);
            }

            public static void ExecutedEndpoint(ILogger logger, Endpoint endpoint)
            {
                _executedEndpoint(logger, endpoint.DisplayName, null);
            }
        }
    }
}

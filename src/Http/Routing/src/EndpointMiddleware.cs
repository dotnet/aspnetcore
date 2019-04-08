// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class EndpointMiddleware
    {
        internal const string AuthorizationMiddlewareInvokedKey = "__AuthorizationMiddlewareInvoked";
        internal const string CorsMiddlewareInvokedKey = "__CorsMiddlewareInvoked";

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public EndpointMiddleware(ILogger<EndpointMiddleware> logger, RequestDelegate next)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
            if (endpoint?.RequestDelegate != null)
            {
                EnsureRequisiteMiddlewares(httpContext, endpoint);

                Log.ExecutingEndpoint(_logger, endpoint);

                try
                {
                    await endpoint.RequestDelegate(httpContext);
                }
                finally
                {
                    Log.ExecutedEndpoint(_logger, endpoint);
                }

                return;
            }

            await _next(httpContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureRequisiteMiddlewares(HttpContext httpContext, Endpoint endpoint)
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

        private static void ThrowMissingAuthMiddlewareException(Endpoint endpoint)
        {
            throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains authorization metadata, " +
                "but a middleware was not found that supports authorization.");
        }

        private static void ThrowMissingCorsMiddlewareException(Endpoint endpoint)
        {
            throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains CORS metadata, " +
                "but a middleware was not found that supports CORS.");
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

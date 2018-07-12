// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class EndpointMiddleware
    {
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
            var feature = httpContext.Features.Get<IEndpointFeature>();
            if (feature == null)
            {
                throw new InvalidOperationException("Unable to execute an endpoint because the dispatcher was not run. Ensure dispatcher middleware is registered.");
            }

            if (feature.Invoker != null)
            {
                Log.ExecutingEndpoint(_logger, feature.Endpoint);

                try
                {
                    await feature.Invoker(_next)(httpContext);
                }
                finally
                {
                    Log.ExecutedEndpoint(_logger, feature.Endpoint);
                }

                return;
            }

            await _next(httpContext);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _executingEndpoint = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(0, "ExecutingEndpoint"),
                "Executing endpoint '{EndpointName}'.");

            private static readonly Action<ILogger, string, Exception> _executedEndpoint = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, "ExecutedEndpoint"),
                "Executed endpoint '{EndpointName}'.");

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    // We use a middleware so that we can use DI.
    internal class CircuitDisconnectMiddleware
    {
        private const string CircuitIdKey = "circuitId";

        public CircuitDisconnectMiddleware(
            ILogger<CircuitDisconnectMiddleware> logger,
            CircuitRegistry registry,
            CircuitIdFactory circuitIdFactory,
            RequestDelegate next)
        {
            Logger = logger;
            Registry = registry;
            CircuitIdFactory = circuitIdFactory;
            Next = next;
        }

        public ILogger<CircuitDisconnectMiddleware> Logger { get; }
        public CircuitRegistry Registry { get; }
        public CircuitIdFactory CircuitIdFactory { get; }
        public RequestDelegate Next { get; }

        public async Task Invoke(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            var circuitId = await GetCircuitIdAsync(context);
            if (circuitId is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await TerminateCircuitGracefully(circuitId.Value);

            context.Response.StatusCode = StatusCodes.Status200OK;
        }

        private async Task<CircuitId?> GetCircuitIdAsync(HttpContext context)
        {
            try
            {
                if (!context.Request.HasFormContentType)
                {
                    return default;
                }

                var form = await context.Request.ReadFormAsync();
                if (!form.TryGetValue(CircuitIdKey, out var text))
                {
                    return default;
                }

                if (!CircuitIdFactory.TryParseCircuitId(text, out var circuitId))
                {
                    Log.InvalidCircuitId(Logger, text);
                    return default;
                }

                return circuitId;
            }
            catch
            {
                return default;
            }
        }

        private async Task TerminateCircuitGracefully(CircuitId circuitId)
        {
            // We don't expect TerminateAsync to throw.
            Log.CircuitTerminatingGracefully(Logger, circuitId);
            await Registry.TerminateAsync(circuitId);
            Log.CircuitTerminatedGracefully(Logger, circuitId);
        }

        private class Log
        {
            private static readonly Action<ILogger, CircuitId, Exception> _circuitTerminatingGracefully =
                LoggerMessage.Define<CircuitId>(LogLevel.Debug, new EventId(1, "CircuitTerminatingGracefully"), "Circuit with id '{CircuitId}' terminating gracefully.");

            private static readonly Action<ILogger, CircuitId, Exception> _circuitTerminatedGracefully =
                LoggerMessage.Define<CircuitId>(LogLevel.Debug, new EventId(2, "CircuitTerminatedGracefully"), "Circuit with id '{CircuitId}' terminated gracefully.");

            private static readonly Action<ILogger, string, Exception> _invalidCircuitId =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "InvalidCircuitId"), "CircuitDisconnectMiddleware received an invalid circuit id '{CircuitIdSecret}'.");

            public static void CircuitTerminatingGracefully(ILogger logger, CircuitId circuitId) => _circuitTerminatingGracefully(logger, circuitId, null);

            public static void CircuitTerminatedGracefully(ILogger logger, CircuitId circuitId) => _circuitTerminatedGracefully(logger, circuitId, null);

            public static void InvalidCircuitId(ILogger logger, string circuitSecret) => _invalidCircuitId(logger, circuitSecret, null);
        }
    }
}

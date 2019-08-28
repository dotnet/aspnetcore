// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    // We use a middlware so that we can use DI.
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

            var (hasCircuitId, circuitId) = await TryGetCircuitIdAsync(context);
            if (!hasCircuitId)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await TerminateCircuitGracefully(circuitId);

            context.Response.StatusCode = StatusCodes.Status200OK;
        }

        private async Task<(bool, string)> TryGetCircuitIdAsync(HttpContext context)
        {
            try
            {
                if (!context.Request.HasFormContentType)
                {
                    return (false, null);
                }

                var form = await context.Request.ReadFormAsync();
                if (!form.TryGetValue(CircuitIdKey, out var circuitId) || !CircuitIdFactory.ValidateCircuitId(circuitId))
                {
                    return (false, null);
                }

                return (true, circuitId);
            }
            catch
            {
                return (false, null);
            }
        }

        private async Task TerminateCircuitGracefully(string circuitId)
        {
            try
            {
                await Registry.TerminateAsync(circuitId);
                Log.CircuitTerminatedGracefully(Logger, circuitId);
            }
            catch (Exception e)
            {
                Log.UnhandledExceptionInCircuit(Logger, circuitId, e);
            }
        }

        private class Log
        {
            private static readonly Action<ILogger, string, Exception> _circuitTerminatedGracefully =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "CircuitTerminatedGracefully"), "Circuit '{CircuitId}' terminated gracefully");

            private static readonly Action<ILogger, string, Exception> _unhandledExceptionInCircuit =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, "UnhandledExceptionInCircuit"), "Unhandled exception in circuit {CircuitId} while terminating gracefully.");

            public static void CircuitTerminatedGracefully(ILogger logger, string circuitId) => _circuitTerminatedGracefully(logger, circuitId, null);

            public static void UnhandledExceptionInCircuit(ILogger logger, string circuitId, Exception exception) => _unhandledExceptionInCircuit(logger, circuitId, exception);
        }
    }
}

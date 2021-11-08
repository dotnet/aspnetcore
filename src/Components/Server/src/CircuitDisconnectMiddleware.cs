// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server;

// We use a middleware so that we can use DI.
internal sealed partial class CircuitDisconnectMiddleware
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

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Circuit with id '{CircuitId}' terminating gracefully.", EventName = "CircuitTerminatingGracefully")]
        public static partial void CircuitTerminatingGracefully(ILogger logger, CircuitId circuitId);

        [LoggerMessage(2, LogLevel.Debug, "Circuit with id '{CircuitId}' terminated gracefully.", EventName = "CircuitTerminatedGracefully")]
        public static partial void CircuitTerminatedGracefully(ILogger logger, CircuitId circuitId);

        [LoggerMessage(3, LogLevel.Debug, "CircuitDisconnectMiddleware received an invalid circuit id '{CircuitIdSecret}'.", EventName = "InvalidCircuitId")]
        public static partial void InvalidCircuitId(ILogger logger, string circuitIdSecret);
    }
}

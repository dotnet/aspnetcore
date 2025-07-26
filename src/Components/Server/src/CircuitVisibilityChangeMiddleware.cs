// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Handles the beacon message that indicates that the visibility of the page has changed.
/// This information is used as a heuristic for determining whether the circuit should be kept around when disconnected.
/// See <see cref="CircuitRegistry.CircuitQualifiesForTermination(CircuitHost)"/> for more details.
/// </summary>
/// <remarks>
/// We use a middleware so that we can use DI.
/// </remarks>
internal sealed partial class CircuitVisibilityChangeMiddleware
{
    private const string CircuitIdKey = "circuitId";

    public CircuitVisibilityChangeMiddleware(
        ILogger<CircuitVisibilityChangeMiddleware> logger,
        CircuitRegistry registry,
        CircuitIdFactory circuitIdFactory,
        RequestDelegate next)
    {
        Logger = logger;
        Registry = registry;
        CircuitIdFactory = circuitIdFactory;
        Next = next;
    }

    public ILogger<CircuitVisibilityChangeMiddleware> Logger { get; }
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

        var beaconData = await GetBeaconDataAsync(context);
        if (beaconData is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Registry.UpdatePageHiddenTimestamp(beaconData.Value.CircuitId, beaconData.Value.IsVisible);

        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    private async Task<VisibilityChangeBeacon?> GetBeaconDataAsync(HttpContext context)
    {
        try
        {
            if (!context.Request.HasFormContentType)
            {
                return default;
            }

            var form = await context.Request.ReadFormAsync();

            if (!form.TryGetValue(CircuitIdKey, out var circuitIdText))
            {
                return default;
            }

            if (!CircuitIdFactory.TryParseCircuitId(circuitIdText, out var circuitId))
            {
                Log.InvalidCircuitId(Logger, circuitIdText);
                return default;
            }

            if (!form.TryGetValue("isVisible", out var isVisibleText)
                || !bool.TryParse(isVisibleText, out var isVisible))
            {
                 return default;
            }

            return new VisibilityChangeBeacon
            {
                CircuitId = circuitId,
                IsVisible = isVisible
            };
        }
        catch
        {
            return default;
        }
    }

    private readonly struct VisibilityChangeBeacon
    {
        public required CircuitId CircuitId { get; init; }

        public required bool IsVisible { get; init; }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "CircuitDisconnectMiddleware received an invalid circuit id '{CircuitIdSecret}'.", EventName = "InvalidCircuitId")]
        public static partial void InvalidCircuitId(ILogger logger, string circuitIdSecret);
    }
}

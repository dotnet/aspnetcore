// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class SessionStorageTempDataProvider : ITempDataProvider
{
    internal const string TempDataSessionStateKey = "__BlazorTempData";
    private readonly ITempDataSerializer _tempDataSerializer;
    private readonly ILogger<SessionStorageTempDataProvider> _logger;

    public SessionStorageTempDataProvider(
        ITempDataSerializer tempDataSerializer,
        ILogger<SessionStorageTempDataProvider> logger)
    {
        _tempDataSerializer = tempDataSerializer;
        _logger = logger;
    }

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var session = context.Session;

            if (session.TryGetValue(TempDataSessionStateKey, out var value))
            {
                var dataFromSession = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                if (dataFromSession is null)
                {
                    return new Dictionary<string, object?>();
                }

                var convertedData = new Dictionary<string, object?>();
                foreach (var kvp in dataFromSession)
                {
                    convertedData[kvp.Key] = _tempDataSerializer.Deserialize(kvp.Value);
                }
                Log.TempDataSessionLoadSuccess(_logger);
                return convertedData;
            }

            Log.TempDataSessionNotFound(_logger);
            return new Dictionary<string, object?>();
        }
        catch (Exception ex)
        {
            Log.TempDataSessionLoadFailure(_logger, ex);
            return new Dictionary<string, object?>();
        }
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var kvp in values)
        {
            if (!_tempDataSerializer.EnsureObjectCanBeSerialized(kvp.Value?.GetType() ?? typeof(object)))
            {
                throw new InvalidOperationException($"TempData cannot store values of type '{kvp.Value?.GetType()}'.");
            }
        }

        var session = context.Session;
        if (values.Count == 0)
        {
            session.Remove(TempDataSessionStateKey);
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(values);
        session.Set(TempDataSessionStateKey, bytes);
        Log.TempDataSessionSaveSuccess(_logger);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "TempData was not found in session.", EventName = "TempDataSessionNotFound")]
        public static partial void TempDataSessionNotFound(ILogger logger);

        [LoggerMessage(2, LogLevel.Warning, "TempData could not be loaded from session.", EventName = "TempDataSessionLoadFailure")]
        public static partial void TempDataSessionLoadFailure(ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "TempData was successfully saved to session.", EventName = "TempDataSessionSaveSuccess")]
        public static partial void TempDataSessionSaveSuccess(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "TempData was successfully loaded from session.", EventName = "TempDataSessionLoadSuccess")]
        public static partial void TempDataSessionLoadSuccess(ILogger logger);
    }
}

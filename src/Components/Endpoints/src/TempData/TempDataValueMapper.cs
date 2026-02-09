// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class TempDataValueMapper : ITempDataValueMapper
{
    private HttpContext? _httpContext;
    private readonly Dictionary<string, List<Func<object?>>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TempDataValueMapper> _logger;

    public TempDataValueMapper(ILogger<TempDataValueMapper> logger)
    {
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public object? GetValue(string tempDataKey, Type type)
    {
        var tempData = GetTempData();
        if (tempData is null)
        {
            return null;
        }

        try
        {
            var value = tempData.Get(tempDataKey);
            if (value is null)
            {
                return null;
            }
            return value;
        }
        catch (JsonException ex)
        {
            Log.TempDataDeserializeFail(_logger, ex);
            return null;
        }
    }

    public void RegisterValueCallback(string tempDataKey, Func<object?> valueGetter)
    {
        if (!_valueCallbacks.TryGetValue(tempDataKey, out var callbacks))
        {
            callbacks = new List<Func<object?>>();
            _valueCallbacks[tempDataKey] = callbacks;
        }

        callbacks.Add(valueGetter);
    }

    private ITempData? GetTempData()
    {
        if (_httpContext is null)
        {
            return null;
        }

        var key = typeof(ITempData);
        if (_httpContext.Items.TryGetValue(key, out var tempDataObj) && tempDataObj is ITempData tempData)
        {
            return tempData;
        }

        return null;
    }

    internal void PersistValues(ITempData tempData)
    {
        foreach (var (key, callbacks) in _valueCallbacks)
        {
            object? value = null;
            foreach (var valueGetter in callbacks)
            {
                try
                {
                    var candidateValue = valueGetter();
                    if (candidateValue is not null)
                    {
                        value = candidateValue;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.TempDataPersistFail(_logger, ex);
                }
            }
            tempData[key] = value;
        }
    }

    public void DeleteValueCallback(string tempDataKey)
    {
        _valueCallbacks.Remove(tempDataKey);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Persisting of the TempData element failed.", EventName = "TempDataPersistFail")]
        public static partial void TempDataPersistFail(ILogger logger, Exception exception);

        [LoggerMessage(2, LogLevel.Warning, "Deserialization of the element from TempData failed.", EventName = "TempDataDeserializeFail")]
        public static partial void TempDataDeserializeFail(ILogger logger, JsonException exception);
    }
}

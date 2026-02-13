// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class TempDataValueMapper : ITempDataValueMapper
{
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TempDataValueMapper> _logger;

    public TempDataValueMapper(ILogger<TempDataValueMapper> logger)
    {
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public object? GetValue(string tempDataKey, Type targetType)
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

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (underlyingType.IsEnum && value is int intValue)
            {
                return Enum.ToObject(underlyingType, intValue);
            }

            if (!underlyingType.IsAssignableFrom(value.GetType()))
            {
                return null;
            }

            return value;
        }
        catch (Exception ex)
        {
            Log.TempDataDeserializeFail(_logger, ex);
            return null;
        }
    }

    public void RegisterValueCallback(string tempDataKey, Func<object?> valueGetter)
    {
        if (!_valueCallbacks.TryAdd(tempDataKey, valueGetter))
        {
            throw new InvalidOperationException($"A callback is already registered for the TempData key '{tempDataKey}'. Multiple components cannot use the same TempData key.");
        }
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
        foreach (var (key, valueGetter) in _valueCallbacks)
        {
            object? value = null;
            try
            {
                value = valueGetter();
            }
            catch (Exception ex)
            {
                Log.TempDataPersistFail(_logger, ex);
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
        public static partial void TempDataDeserializeFail(ILogger logger, Exception exception);
    }
}

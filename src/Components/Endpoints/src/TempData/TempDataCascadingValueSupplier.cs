// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class TempDataCascadingValueSupplier
{
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TempDataCascadingValueSupplier> _logger;

    public TempDataCascadingValueSupplier(ILogger<TempDataCascadingValueSupplier> logger)
    {
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    internal CascadingParameterSubscription CreateSubscription(
        ComponentState componentState,
        SupplyParameterFromTempDataAttribute attribute,
        CascadingParameterInfo parameterInfo)
    {
        var tempDataKey = attribute.Name ?? parameterInfo.PropertyName;
        var componentType = componentState.Component.GetType();
        var propertyInfo = componentType.GetProperty(
            parameterInfo.PropertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        this.RegisterValueCallback(tempDataKey, () =>
        {
            var value = propertyInfo?.GetValue(componentState.Component);
            return value;
        });
        return new TempDataSubscription(this, tempDataKey, parameterInfo.PropertyType);
    }

    internal object? GetValue(string tempDataKey, Type targetType)
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

    internal void RegisterValueCallback(string tempDataKey, Func<object?> valueGetter)
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
        if (_valueCallbacks.Count == 0)
        {
            return;
        }

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
                continue;
            }
            tempData[key] = value;
        }
    }

    internal void DeleteValueCallback(string tempDataKey)
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

    private partial class TempDataSubscription : CascadingParameterSubscription
    {
        private readonly TempDataCascadingValueSupplier _owner;
        private readonly string _tempDataKey;
        private readonly Type _propertyType;

        public TempDataSubscription(TempDataCascadingValueSupplier owner, string tempDataKey, Type propertyType)
        {
            _owner = owner;
            _tempDataKey = tempDataKey;
            _propertyType = propertyType;
        }

        public override object? GetCurrentValue()
        {
            return _owner.GetValue(_tempDataKey, _propertyType);
        }

        public override void Dispose()
        {
            _owner.DeleteValueCallback(_tempDataKey);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class TempDataCascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();
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
        var getter = _propertyGetterCache.GetOrAdd((componentType, parameterInfo.PropertyName), PropertyGetterFactory);
        Func<object?> valueGetter = () => getter.GetValue(componentState.Component);
        RegisterValueCallback(tempDataKey, valueGetter);
        return new TempDataSubscription(this, tempDataKey, parameterInfo.PropertyType, valueGetter);
    }

    private static PropertyGetter PropertyGetterFactory((Type type, string propertyName) key)
    {
        var (type, propertyName) = key;
        var propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo is null)
        {
            throw new InvalidOperationException($"A property '{propertyName}' on component type '{type.FullName}' wasn't found.");
        }
        return new PropertyGetter(type, propertyInfo);
    }

    internal ITempData? GetTempData() => _httpContext is null
        ? null
        : TempDataProviderServiceCollectionExtensions.GetOrCreateTempData(_httpContext);

    internal void RegisterValueCallback(string tempDataKey, Func<object?> valueGetter)
    {
        if (!_valueCallbacks.TryAdd(tempDataKey, valueGetter))
        {
            throw new InvalidOperationException($"A callback is already registered for the TempData key '{tempDataKey}'. Multiple components cannot use the same TempData key for multiple [SupplyParameterFromTempData] attributes.");
        }
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

    internal partial class TempDataSubscription : CascadingParameterSubscription
    {
        private readonly TempDataCascadingValueSupplier _owner;
        private readonly string _tempDataKey;
        private readonly Type _underlyingType;
        private readonly bool _isEnum;
        private readonly Func<object?> _currentValueGetter;
        private bool _delivered;

        public TempDataSubscription(
            TempDataCascadingValueSupplier owner,
            string tempDataKey,
            Type propertyType,
            Func<object?> currentValueGetter)
        {
            _owner = owner;
            _tempDataKey = tempDataKey;
            _underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            _isEnum = _underlyingType.IsEnum;
            _currentValueGetter = currentValueGetter;
        }

        public override object? GetCurrentValue()
        {
            if (_delivered)
            {
                // After the first delivery, return the component's current property value
                // to avoid overriding modifications the component made during rendering.
                return _currentValueGetter();
            }

            _delivered = true;

            var tempData = _owner.GetTempData();
            if (tempData is null)
            {
                return null;
            }

            try
            {
                var value = tempData.Get(_tempDataKey);
                if (value is null)
                {
                    return null;
                }

                if (_isEnum && value is int intValue)
                {
                    return Enum.ToObject(_underlyingType, intValue);
                }

                if (!_underlyingType.IsAssignableFrom(value.GetType()))
                {
                    return null;
                }

                return value;
            }
            catch (Exception ex)
            {
                Log.TempDataDeserializeFail(_owner._logger, ex);
                return null;
            }
        }

        public override void Dispose()
        {
            _owner.DeleteValueCallback(_tempDataKey);
        }
    }
}

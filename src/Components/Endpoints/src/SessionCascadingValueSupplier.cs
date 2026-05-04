// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class SessionCascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SessionCascadingValueSupplier> _logger;

    public SessionCascadingValueSupplier(ILogger<SessionCascadingValueSupplier> logger)
    {
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
        _httpContext.Response.OnStarting(PersistAllValues);
    }

    internal CascadingParameterSubscription CreateSubscription(
        ComponentState componentState,
        SupplyParameterFromSessionAttribute attribute,
        CascadingParameterInfo parameterInfo)
    {
        var sessionKey = attribute.Name ?? parameterInfo.PropertyName;
        var componentType = componentState.Component.GetType();
        var getter = _propertyGetterCache.GetOrAdd((componentType, parameterInfo.PropertyName), PropertyGetterFactory);
        Func<object?> valueGetter = () => getter.GetValue(componentState.Component);
        RegisterValueCallback(sessionKey, valueGetter);
        return new SessionSubscription(this, sessionKey, parameterInfo.PropertyType, valueGetter);
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

    internal ISession? GetSession() => _httpContext?.Features.Get<ISessionFeature>()?.Session;

    internal void RegisterValueCallback(string sessionKey, Func<object?> valueGetter)
    {
        if (!_valueCallbacks.TryAdd(sessionKey, valueGetter))
        {
            throw new InvalidOperationException($"A callback is already registered for the session key '{sessionKey}'. Multiple components cannot use the same session key for multiple [SupplyParameterFromSession] attributes.");
        }
    }

    internal Task PersistAllValues()
    {
        if (_valueCallbacks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var session = GetSession();
        if (session is null)
        {
            return Task.CompletedTask;
        }

        foreach (var (key, valueGetter) in _valueCallbacks)
        {
            var sessionKey = key.ToLowerInvariant();
            try
            {
                var value = valueGetter();
                if (value is not null)
                {
                    var json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
                    session.SetString(sessionKey, json);
                }
                else
                {
                    session.Remove(sessionKey);
                }
            }
            catch (Exception ex)
            {
                Log.SessionPersistFail(_logger, ex);
            }
        }
        return Task.CompletedTask;
    }

    internal void DeleteValueCallback(string sessionKey)
    {
        _valueCallbacks.Remove(sessionKey);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Persisting of the session element failed.", EventName = "SessionPersistFail")]
        public static partial void SessionPersistFail(ILogger logger, Exception exception);

        [LoggerMessage(2, LogLevel.Warning, "Deserialization of the element from session failed.", EventName = "SessionDeserializeFail")]
        public static partial void SessionDeserializeFail(ILogger logger, Exception exception);
    }

    internal partial class SessionSubscription : CascadingParameterSubscription
    {
        private readonly SessionCascadingValueSupplier _owner;
        private readonly string _sessionKey;
        private readonly Type _propertyType;
        private readonly Func<object?> _currentValueGetter;
        private bool _delivered;

        public SessionSubscription(
            SessionCascadingValueSupplier owner,
            string sessionKey,
            Type propertyType,
            Func<object?> currentValueGetter)
        {
            _owner = owner;
            _sessionKey = sessionKey;
            _propertyType = propertyType;
            _currentValueGetter = currentValueGetter;
        }

        public override object? GetCurrentValue()
        {
            if (_delivered)
            {
                return _currentValueGetter();
            }

            _delivered = true;
            var session = _owner.GetSession();
            if (session is null)
            {
                return null;
            }

            try
            {
                var json = session.GetString(_sessionKey.ToLowerInvariant());
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                return JsonSerializer.Deserialize(json, _propertyType, _jsonOptions);
            }
            catch (Exception ex)
            {
                Log.SessionDeserializeFail(_owner._logger, ex);
                return null;
            }
        }

        public override void Dispose()
        {
            _owner.DeleteValueCallback(_sessionKey);
        }
    }
}

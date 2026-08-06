// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class SessionCascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly IStoredDataSerializer _serializer;
    private readonly ILogger<SessionCascadingValueSupplier> _logger;

    public SessionCascadingValueSupplier(IStoredDataSerializer serializer, ILogger<SessionCascadingValueSupplier> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    internal CascadingParameterSubscription CreateSubscription(
        ComponentState componentState,
        SupplyParameterFromSessionAttribute attribute,
        CascadingParameterInfo parameterInfo)
    {
        if (_httpContext is not null)
        {
            // Ensure that session cookie is issued to allow for persistence from streaming context
            SessionEstablishmentHelper.TryRegisterSessionEstablishment(_httpContext);
        }

        var sessionKey = (attribute.Name ?? parameterInfo.PropertyName).ToLowerInvariant();
        var componentType = componentState.Component.GetType();
        var propertyType = parameterInfo.PropertyType;
        if (propertyType != typeof(object) && !propertyType.IsAbstract && !_serializer.CanSerialize(propertyType))
        {
            throw new InvalidOperationException(
                $"The property '{parameterInfo.PropertyName}' on component '{componentType}' is annotated with '[SupplyParameterFromSession]' but its type '{propertyType}' is not supported for session storage.");
        }

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

    // A null HttpContext means we're rendering interactively (Server circuit or WebAssembly),
    // where the session isn't available; yield null instead of failing. When an HttpContext is
    // present (static SSR) an unavailable session is a misconfiguration and fails fast.
    internal ISession? GetSession()
        => _httpContext is null ? null : SessionResolver.GetRequiredSession(_httpContext);

    internal void RegisterValueCallback(string sessionKey, Func<object?> valueGetter)
    {
        sessionKey = sessionKey.ToLowerInvariant();
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
            Log.SessionUnavailable(_logger);
            return Task.CompletedTask;
        }

        foreach (var (key, valueGetter) in _valueCallbacks)
        {
            object? value;
            try
            {
                value = valueGetter();
            }
            catch (Exception ex)
            {
                Log.SessionPersistFail(_logger, ex);
                continue;
            }

            if (value is null)
            {
                session.Remove(key);
                continue;
            }

            var valueType = value.GetType();
            var bytes = _serializer.SerializeValue(value, valueType);

            session.Set(key, bytes);
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

        [LoggerMessage(3, LogLevel.Warning, "No active HttpContext is available (interactive rendering); [SupplyParameterFromSession] is skipped.", EventName = "SessionUnavailable")]
        public static partial void SessionUnavailable(ILogger logger);
    }

    internal partial class SessionSubscription : CascadingParameterSubscription
    {
        private readonly SessionCascadingValueSupplier _owner;
        private readonly string _sessionKey;
        [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)]
        private readonly Type _propertyType;
        private readonly Func<object?> _currentValueGetter;
        private bool _delivered;

        public SessionSubscription(
            SessionCascadingValueSupplier owner,
            string sessionKey,
            [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] Type propertyType,
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
                Log.SessionUnavailable(_owner._logger);
                return null;
            }

            try
            {
                if (!session.TryGetValue(_sessionKey, out var bytes) || bytes.Length == 0)
                {
                    return null;
                }

                // The property type is known, so the serializer deserializes straight to it and recovers
                // the exact enum/collection type without consulting the stored token.
                return _owner._serializer.DeserializeValue(bytes, _propertyType);
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

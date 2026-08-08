// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class SessionCascadingValueSupplier
{
    private readonly JsonSerializerOptions _jsonOptions;
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SessionCascadingValueSupplier> _logger;

    public SessionCascadingValueSupplier(
        ILogger<SessionCascadingValueSupplier> logger,
        IEnumerable<RazorComponentsMetadataContext>? metadataContexts = null)
    {
        _logger = logger;
        _jsonOptions = CreateJsonOptions(metadataContexts);
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

        var sessionKey = attribute.Name ?? parameterInfo.PropertyName;
        var valueGetter = ComponentParameterValueGetter.Create(componentState, parameterInfo.PropertyName);
        RegisterValueCallback(sessionKey, valueGetter);
        return new SessionSubscription(this, sessionKey, parameterInfo.PropertyType, valueGetter);
    }

    // A null HttpContext means we're rendering interactively (Server circuit or WebAssembly),
    // where the session isn't available; yield null instead of failing. When an HttpContext is
    // present (static SSR) an unavailable session is a misconfiguration and fails fast.
    internal ISession? GetSession()
        => _httpContext is null ? null : SessionResolver.GetRequiredSession(_httpContext);

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
            Log.SessionUnavailable(_logger);
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
                    var typeInfo = _jsonOptions.GetTypeInfo(value.GetType());
                    var json = JsonSerializer.Serialize(value, typeInfo);
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

        [LoggerMessage(3, LogLevel.Warning, "No active HttpContext is available (interactive rendering); [SupplyParameterFromSession] is skipped.", EventName = "SessionUnavailable")]
        public static partial void SessionUnavailable(ILogger logger);
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
                Log.SessionUnavailable(_owner._logger);
                return null;
            }

            try
            {
                var json = session.GetString(_sessionKey.ToLowerInvariant());
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                var typeInfo = _owner._jsonOptions.GetTypeInfo(_propertyType);
                return JsonSerializer.Deserialize(json, typeInfo);
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

    private static JsonSerializerOptions CreateJsonOptions(IEnumerable<RazorComponentsMetadataContext>? metadataContexts)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        if (metadataContexts is not null)
        {
            foreach (var context in metadataContexts)
            {
                if (context.JsonTypeInfoResolver is { } resolver &&
                    !options.TypeInfoResolverChain.Contains(resolver))
                {
                    options.TypeInfoResolverChain.Add(resolver);
                }
            }
        }

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }

        return options;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class SessionValueMapper : ISessionValueMapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SessionValueMapper> _logger;

    public SessionValueMapper(ILogger<SessionValueMapper> logger)
    {
        _logger = logger;
    }

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
        _httpContext.Response.OnStarting(PersistAllValues);
    }

    public object? GetValue(string sessionKey, Type type)
    {
        var session = _httpContext?.Features.Get<ISessionFeature>()?.Session;
        if (session is null)
        {
            return null;
        }
        try
        {
            var json = session.GetString(sessionKey.ToLowerInvariant());
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            return JsonSerializer.Deserialize(json, type, _jsonOptions);
        }
        catch (JsonException ex)
        {
            Log.SessionDeserializeFail(_logger, ex);
            return null;
        }
    }

    public void RegisterValueCallback(string sessionKey, Func<object?> valueGetter)
    {
        if (!_valueCallbacks.TryAdd(sessionKey, valueGetter))
        {
            throw new InvalidOperationException($"A callback is already registered for the session key '{sessionKey}'. Multiple components cannot use the same session key.");
        }
    }

    public void DeleteValueCallback(string sessionKey)
    {
        _valueCallbacks.Remove(sessionKey);
    }

    private Task PersistAllValues()
    {
        var session = _httpContext?.Features.Get<ISessionFeature>()?.Session;
        if (session is null)
        {
            return Task.CompletedTask;
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
                Log.SessionPersistFail(_logger, ex);
            }
            if (value is not null)
            {
                var json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
                session.SetString(key.ToLowerInvariant(), json);
            }
            else
            {
                session.Remove(key.ToLowerInvariant());
            }
        }
        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Persisting of the element failed.", EventName = "SessionPersistFail")]
        public static partial void SessionPersistFail(ILogger logger, Exception exception);

        [LoggerMessage(2, LogLevel.Warning, "Deserialization of the element from Session failed.", EventName = "SessionDeserializeFail")]
        public static partial void SessionDeserializeFail(ILogger logger, JsonException exception);
    }
}

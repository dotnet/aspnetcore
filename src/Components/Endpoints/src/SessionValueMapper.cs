// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class SessionValueMapper : ISessionValueMapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpContext? _httpContext;
    private readonly Dictionary<string, Func<object?>> _valueCallbacks = new();

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;

        // Register Response.OnStarting once to persist all values before response starts
        _httpContext.Response.OnStarting(PersistAllValues);
    }

    public object? GetValue(string sessionKey, Type type)
    {
        var session = _httpContext?.Session;
        if (session is null)
        {
            return null;
        }
        var json = session.GetString(sessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        return JsonSerializer.Deserialize(json, type, _jsonOptions);
    }

    public void RegisterValueCallback(string sessionKey, Func<object?> valueGetter)
    {
        _valueCallbacks[sessionKey] = valueGetter;
    }

    public void SetValue(string sessionKey, object? value)
    {
        var session = _httpContext?.Session;
        if (session is null)
        {
            return;
        }
        if (value is null)
        {
            session.Remove(sessionKey);
        }
        else
        {
            var json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
            session.SetString(sessionKey, json);
        }
    }

    private Task PersistAllValues()
    {
        var session = _httpContext?.Session;
        if (session is null)
        {
            return Task.CompletedTask;
        }

        foreach (var (key, valueGetter) in _valueCallbacks)
        {
            var value = valueGetter();
            if (value is not null)
            {
                var json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
                session.SetString(key, json);
            }
            else
            {
                session.Remove(key);
            }
        }

        return Task.CompletedTask;
    }
}

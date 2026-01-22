// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class SessionValueMapper : ISessionValueMapper
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpContext? _httpContext;

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public object? GetValue(string sessionKey, Type type)
    {
        var session = _httpContext?.Session;
        if (session is null)
        {
            return null;
        }
        var json = session.GetString(sessionKey);
        if (String.IsNullOrEmpty(json))
        {
            return null;
        }
        return JsonSerializer.Deserialize(json, type, _jsonOptions);
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class SessionValueMapper : ISessionValueMapper
{
    private HttpContext? _httpContext;

    internal void SetRequestContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public object? GetValue(string sessionKey)
    {
        var session = _httpContext?.Session;
        if (session is null)
        {
            return null;
        }
        return session.GetString(sessionKey);
    }
}

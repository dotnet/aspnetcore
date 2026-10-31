// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class SessionResolver
{
    // Matches the message thrown by HttpContext.Session so both session-consuming
    // features surface the same InvalidOperationException when the session is unavailable.
    internal const string SessionNotConfiguredMessage = "Session has not been configured for this application or request.";

    internal static ISession GetRequiredSession(HttpContext httpContext)
    {
        var session = httpContext.Session;
        if (session is null)
        {
            throw new InvalidOperationException(SessionNotConfiguredMessage);
        }

        return session;
    }
}

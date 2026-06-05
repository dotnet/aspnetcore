// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class SessionEstablishmentHelper
{
    private const string SessionEstablishmentKey = "__AspNetCore.Components.Endpoints.SessionEstablishment";

    public static void TryRegisterSessionEstablishment(HttpContext context)
    {
        var session = context.Features.Get<ISessionFeature>()?.Session;
        if (session is null || context.Response.HasStarted)
        {
            return;
        }

        if (context.Items.ContainsKey(SessionEstablishmentKey))
        {
            return;
        }
        context.Items[SessionEstablishmentKey] = true;

        context.Response.OnStarting(static state =>
        {
            var session = (ISession)state!;
            session.Set(SessionEstablishmentKey, []);
            session.Remove(SessionEstablishmentKey);
            return Task.CompletedTask;
        }, session);
    }
}

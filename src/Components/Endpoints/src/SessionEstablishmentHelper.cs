// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static partial class SessionEstablishmentHelper
{
    private const string SessionEstablishmentKey = "__AspNetCore.Components.Endpoints.SessionEstablishment";

    public static void TryRegisterSessionEstablishment(HttpContext context)
    {
        var session = context.Features.Get<ISessionFeature>()?.Session;
        if (session is null || context.Response.HasStarted)
        {
            var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                Log.SessionStateNotPersistedAfterResponseStarted(loggerFactory.CreateLogger(typeof(SessionEstablishmentHelper).FullName!));
            }
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

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning,
            "Session is not present or session-backed state was first accessed after the response had already started, so the session cookie can no longer be issued. " +
            "The value will not be persisted on the next request unless the session cookie was established earlier — for example by an earlier [SupplyParameterFromSession], a cascading session-storage TempData resolution, or a direct ISession.Set call before the first flush.",
            EventName = "SessionStateNotPersistedAfterResponseStarted")]
        public static partial void SessionStateNotPersistedAfterResponseStarted(ILogger logger);
    }
}

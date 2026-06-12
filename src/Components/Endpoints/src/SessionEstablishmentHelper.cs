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

    private static bool HasLoggedSessionStateNotPersistedAfterResponseStarted = false;

    public static void TryRegisterSessionEstablishment(HttpContext context)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var session = context.Features.Get<ISessionFeature>()?.Session;
        if (session == null)
        {
            Log.SessionDoesNotExist(loggerFactory.CreateLogger(typeof(SessionEstablishmentHelper).FullName!));
            return;
        }
        if (context.Response.HasStarted)
        {
            HasLoggedSessionStateNotPersistedAfterResponseStarted = true;
            Log.ResponseHasStarted(loggerFactory.CreateLogger(typeof(SessionEstablishmentHelper).FullName!));
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
            "Session state was not persisted to the next request. " +
            "The response has already started, so the session cookie can no longer be issued. " +
            "To avoid this, access session-backed state before the first response flush — for example by using [SupplyParameterFromSession], accessing session-backed TempData, or calling ISession.Set before any output is written.",
            EventName = "SessionStateNotPersistedAfterResponseStarted")]
        public static partial void ResponseHasStarted(ILogger logger);

        [LoggerMessage(2, LogLevel.Warning,
            "Session state was not persisted to the next request. " +
            "No session is available — either session middleware was not registered, or the component is running interactively where an HTTP session cannot be established. " +
            "To fix this, add 'builder.Services.AddSession()' and 'app.UseSession()' to your app, and ensure session-backed state is first accessed during static SSR rendering.",
            EventName = "SessionDoesNotExist")]
        public static partial void SessionDoesNotExist(ILogger logger);
    }
}

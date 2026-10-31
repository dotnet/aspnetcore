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
    private const string LoggedResponseHasStartedKey = "__AspNetCore.Components.Endpoints.SessionEstablishment.LoggedResponseHasStarted";
    private const string LoggedSessionDoesNotExistKey = "__AspNetCore.Components.Endpoints.SessionEstablishment.LoggedSessionDoesNotExist";

    public static void TryRegisterSessionEstablishment(HttpContext context)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var session = context.Features.Get<ISessionFeature>()?.Session;

        if (session == null)
        {
            if (!context.Items.ContainsKey(LoggedSessionDoesNotExistKey))
            {
                Log.SessionDoesNotExist(loggerFactory.CreateLogger(typeof(SessionEstablishmentHelper).FullName!));
                context.Items[LoggedSessionDoesNotExistKey] = true;
            }
            return;
        }

        if (context.Response.HasStarted)
        {
            if (!context.Items.ContainsKey(LoggedResponseHasStartedKey))
            {
                Log.ResponseHasStarted(loggerFactory.CreateLogger(typeof(SessionEstablishmentHelper).FullName!));
                context.Items[LoggedResponseHasStartedKey] = true;
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
            "Session state was not persisted to the next request. " +
            "The response has already started, so the session cookie can no longer be issued. " +
            "To avoid this, place at least one [SupplyParameterFromSession] (or use Session.Set directly) before any await that triggers the first response flush.",
            EventName = "SessionStateNotPersistedAfterResponseStarted")]
        public static partial void ResponseHasStarted(ILogger logger);

        [LoggerMessage(2, LogLevel.Warning,
            "Session state was not persisted to the next request. " +
            "No session is available session middleware was not registered. " +
            "To fix this, add 'builder.Services.AddSession()' and 'app.UseSession()' to your app.",
            EventName = "SessionDoesNotExist")]
        public static partial void SessionDoesNotExist(ILogger logger);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _errorClosingTheSession;
        private static Action<ILogger, string, Exception> _accessingExpiredSession;
        private static Action<ILogger, string, string, Exception> _sessionStarted;
        private static Action<ILogger, string, string, int, Exception> _sessionLoaded;
        private static Action<ILogger, string, string, int, Exception> _sessionStored;
        private static Action<ILogger, Exception> _errorUnprotectingCookie;

        static LoggingExtensions()
        {
            _errorClosingTheSession = LoggerMessage.Define(
                eventId: 1,
                logLevel: LogLevel.Error,
                formatString: "Error closing the session.");
            _accessingExpiredSession = LoggerMessage.Define<string>(
                eventId: 2,
                logLevel: LogLevel.Warning,
                formatString: "Accessing expired session; Key:{sessionKey}");
            _sessionStarted = LoggerMessage.Define<string, string>(
                eventId: 3,
                logLevel: LogLevel.Information,
                formatString: "Session started; Key:{sessionKey}, Id:{sessionId}");
            _sessionLoaded = LoggerMessage.Define<string, string, int>(
                eventId: 4,
                logLevel: LogLevel.Debug,
                formatString: "Session loaded; Key:{sessionKey}, Id:{sessionId}, Count:{count}");
            _sessionStored = LoggerMessage.Define<string, string, int>(
                eventId: 5,
                logLevel: LogLevel.Debug,
                formatString: "Session stored; Key:{sessionKey}, Id:{sessionId}, Count:{count}");
            _errorUnprotectingCookie = LoggerMessage.Define(
                eventId: 6,
                logLevel: LogLevel.Warning,
                formatString: "Error unprotecting the session cookie.");
        }

        public static void ErrorClosingTheSession(this ILogger logger, Exception exception)
        {
            _errorClosingTheSession(logger, exception);
        }

        public static void AccessingExpiredSession(this ILogger logger, string sessionKey)
        {
            _accessingExpiredSession(logger, sessionKey, null);
        }

        public static void SessionStarted(this ILogger logger, string sessionKey, string sessionId)
        {
            _sessionStarted(logger, sessionKey, sessionId, null);
        }

        public static void SessionLoaded(this ILogger logger, string sessionKey, string sessionId, int count)
        {
            _sessionLoaded(logger, sessionKey, sessionId, count, null);
        }

        public static void SessionStored(this ILogger logger, string sessionKey, string sessionId, int count)
        {
            _sessionStored(logger, sessionKey, sessionId, count, null);
        }

        public static void ErrorUnprotectingSessionCookie(this ILogger logger, Exception exception)
        {
            _errorUnprotectingCookie(logger, exception);
        }
    }
}

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
        private static Action<ILogger, string, Exception> _sessionCacheReadException;
        private static Action<ILogger, Exception> _errorUnprotectingCookie;
        private static Action<ILogger, Exception> _sessionLoadingTimeout;
        private static Action<ILogger, Exception> _sessionCommitTimeout;
        private static Action<ILogger, Exception> _sessionCommitCanceled;
        private static Action<ILogger, Exception> _sessionRefreshTimeout;
        private static Action<ILogger, Exception> _sessionRefreshCanceled;

        static LoggingExtensions()
        {
            _errorClosingTheSession = LoggerMessage.Define(
                eventId: 1,
                logLevel: LogLevel.Error,
                formatString: "Error closing the session.");
            _accessingExpiredSession = LoggerMessage.Define<string>(
                eventId: 2,
                logLevel: LogLevel.Information,
                formatString: "Accessing expired session, Key:{sessionKey}");
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
            _sessionCacheReadException = LoggerMessage.Define<string>(
                eventId: 6,
                logLevel: LogLevel.Error,
                formatString: "Session cache read exception, Key:{sessionKey}");
            _errorUnprotectingCookie = LoggerMessage.Define(
                eventId: 7,
                logLevel: LogLevel.Warning,
                formatString: "Error unprotecting the session cookie.");
            _sessionLoadingTimeout = LoggerMessage.Define(
                eventId: 8,
                logLevel: LogLevel.Warning,
                formatString: "Loading the session timed out.");
            _sessionCommitTimeout = LoggerMessage.Define(
                eventId: 9,
                logLevel: LogLevel.Warning,
                formatString: "Committing the session timed out.");
            _sessionCommitCanceled = LoggerMessage.Define(
                eventId: 10,
                logLevel: LogLevel.Information,
                formatString: "Committing the session was canceled.");
            _sessionRefreshTimeout = LoggerMessage.Define(
                eventId: 11,
                logLevel: LogLevel.Warning,
                formatString: "Refreshing the session timed out.");
            _sessionRefreshCanceled = LoggerMessage.Define(
                eventId: 12,
                logLevel: LogLevel.Information,
                formatString: "Refreshing the session was canceled.");
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

        public static void SessionCacheReadException(this ILogger logger, string sessionKey, Exception exception)
        {
            _sessionCacheReadException(logger, sessionKey, exception);
        }

        public static void ErrorUnprotectingSessionCookie(this ILogger logger, Exception exception)
        {
            _errorUnprotectingCookie(logger, exception);
        }

        public static void SessionLoadingTimeout(this ILogger logger)
        {
            _sessionLoadingTimeout(logger, null);
        }

        public static void SessionCommitTimeout(this ILogger logger)
        {
            _sessionCommitTimeout(logger, null);
        }

        public static void SessionCommitCanceled(this ILogger logger)
        {
            _sessionCommitCanceled(logger, null);
        }

        public static void SessionRefreshTimeout(this ILogger logger)
        {
            _sessionRefreshTimeout(logger, null);
        }

        public static void SessionRefreshCanceled(this ILogger logger)
        {
            _sessionRefreshCanceled(logger, null);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _errorClosingTheSession;
        private static Action<ILogger, string, Exception> _accessingExpiredSession;
        private static Action<ILogger, string, Exception> _sessionStarted;

        static LoggingExtensions()
        {
            _errorClosingTheSession = LoggerMessage.Define(
                eventId: 1,
                logLevel: LogLevel.Error,
                formatString: "Error closing the session.");
            _accessingExpiredSession = LoggerMessage.Define<string>(
                eventId: 2,
                logLevel: LogLevel.Warning,
                formatString: "Accessing expired session {SessionId}");
            _sessionStarted = LoggerMessage.Define<string>(
                eventId: 3,
                logLevel: LogLevel.Information,
                formatString: "Session {SessionId} started");
        }

        public static void ErrorClosingTheSession(this ILogger logger, Exception exception)
        {
            _errorClosingTheSession(logger, exception);
        }

        public static void AccessingExpiredSession(this ILogger logger, string sessionId)
        {
            _accessingExpiredSession(logger, sessionId, null);
        }

        public static void SessionStarted(this ILogger logger, string sessionId)
        {
            _sessionStarted(logger, sessionId, null);
        }
    }
}

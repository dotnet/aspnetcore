// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, Exception?> _signInWithoutWResult;
        private static readonly Action<ILogger, Exception?> _signInWithoutToken;
        private static readonly Action<ILogger, Exception> _exceptionProcessingMessage;
        private static readonly Action<ILogger, string, Exception?> _malformedRedirectUri;
        private static readonly Action<ILogger, Exception?> _remoteSignOutHandledResponse;
        private static readonly Action<ILogger, Exception?> _remoteSignOutSkipped;
        private static readonly Action<ILogger, Exception?> _remoteSignOut;

        static LoggingExtensions()
        {
            _signInWithoutWResult = LoggerMessage.Define(
                eventId: new EventId(1, "SignInWithoutWResult"),
                logLevel: LogLevel.Debug,
                formatString: "Received a sign-in message without a WResult.");
            _signInWithoutToken = LoggerMessage.Define(
                eventId: new EventId(2, "SignInWithoutToken"),
                logLevel: LogLevel.Debug,
                formatString: "Received a sign-in message without a token.");
            _exceptionProcessingMessage = LoggerMessage.Define(
                eventId: new EventId(3, "ExceptionProcessingMessage"),
                logLevel: LogLevel.Error,
                formatString: "Exception occurred while processing message.");
            _malformedRedirectUri = LoggerMessage.Define<string>(
                eventId: new EventId(4, "MalformedRedirectUri"),
                logLevel: LogLevel.Warning,
                formatString: "The sign-out redirect URI '{0}' is malformed.");
            _remoteSignOutHandledResponse = LoggerMessage.Define(
                eventId: new EventId(5, "RemoteSignOutHandledResponse"),
               logLevel: LogLevel.Debug,
               formatString: "RemoteSignOutContext.HandledResponse");
            _remoteSignOutSkipped = LoggerMessage.Define(
                eventId: new EventId(6, "RemoteSignOutSkipped"),
               logLevel: LogLevel.Debug,
               formatString: "RemoteSignOutContext.Skipped");
            _remoteSignOut = LoggerMessage.Define(
                eventId: new EventId(7, "RemoteSignOut"),
               logLevel: LogLevel.Information,
               formatString: "Remote signout request processed.");
        }

        public static void SignInWithoutWResult(this ILogger logger)
        {
            _signInWithoutWResult(logger, null);
        }

        public static void SignInWithoutToken(this ILogger logger)
        {
            _signInWithoutToken(logger, null);
        }

        public static void ExceptionProcessingMessage(this ILogger logger, Exception ex)
        {
            _exceptionProcessingMessage(logger, ex);
        }

        public static void MalformedRedirectUri(this ILogger logger, string uri)
        {
            _malformedRedirectUri(logger, uri, null);
        }

        public static void RemoteSignOutHandledResponse(this ILogger logger)
        {
            _remoteSignOutHandledResponse(logger, null);
        }

        public static void RemoteSignOutSkipped(this ILogger logger)
        {
            _remoteSignOutSkipped(logger, null);
        }

        public static void RemoteSignOut(this ILogger logger)
        {
            _remoteSignOut(logger, null);
        }
    }
}

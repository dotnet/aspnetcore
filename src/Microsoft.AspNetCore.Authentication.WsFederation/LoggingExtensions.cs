// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _signInWithoutWresult;
        private static Action<ILogger, Exception> _signInWithoutToken;
        private static Action<ILogger, Exception> _exceptionProcessingMessage;
        private static Action<ILogger, string, Exception> _malformedRedirectUri;
        private static Action<ILogger, Exception> _remoteSignOutHandledResponse;
        private static Action<ILogger, Exception> _remoteSignOutSkipped;
        private static Action<ILogger, Exception> _remoteSignOut;

        static LoggingExtensions()
        {
            _signInWithoutWresult = LoggerMessage.Define(
                eventId: 1,
                logLevel: LogLevel.Debug,
                formatString: "Received a sign-in message without a WResult.");
            _signInWithoutToken = LoggerMessage.Define(
                eventId: 2,
                logLevel: LogLevel.Debug,
                formatString: "Received a sign-in message without a token.");
            _exceptionProcessingMessage = LoggerMessage.Define(
                eventId: 3,
                logLevel: LogLevel.Error,
                formatString: "Exception occurred while processing message.");
            _malformedRedirectUri = LoggerMessage.Define<string>(
                eventId: 4,
                logLevel: LogLevel.Warning,
                formatString: "The sign-out redirect URI '{0}' is malformed.");
            _remoteSignOutHandledResponse = LoggerMessage.Define(
               eventId: 5,
               logLevel: LogLevel.Debug,
               formatString: "RemoteSignOutContext.HandledResponse");
            _remoteSignOutSkipped = LoggerMessage.Define(
               eventId: 6,
               logLevel: LogLevel.Debug,
               formatString: "RemoteSignOutContext.Skipped");
            _remoteSignOut = LoggerMessage.Define(
               eventId: 7,
               logLevel: LogLevel.Information,
               formatString: "Remote signout request processed.");
        }

        public static void SignInWithoutWresult(this ILogger logger)
        {
            _signInWithoutWresult(logger, null);
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

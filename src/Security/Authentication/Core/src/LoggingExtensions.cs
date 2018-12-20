// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _authSchemeAuthenticated;
        private static readonly Action<ILogger, string, Exception> _authSchemeNotAuthenticated;
        private static readonly Action<ILogger, string, string, Exception> _authSchemeNotAuthenticatedWithFailure;
        private static readonly Action<ILogger, string, Exception> _authSchemeChallenged;
        private static readonly Action<ILogger, string, Exception> _authSchemeForbidden;
        private static readonly Action<ILogger, string, Exception> _remoteAuthenticationError;
        private static readonly Action<ILogger, Exception> _signInHandled;
        private static readonly Action<ILogger, Exception> _signInSkipped;
        private static readonly Action<ILogger, string, Exception> _correlationPropertyNotFound;
        private static readonly Action<ILogger, string, Exception> _correlationCookieNotFound;
        private static readonly Action<ILogger, string, string, Exception> _unexpectedCorrelationCookieValue;
        private static readonly Action<ILogger, Exception> _accessDeniedError;
        private static readonly Action<ILogger, Exception> _accessDeniedContextHandled;
        private static readonly Action<ILogger, Exception> _accessDeniedContextSkipped;

        static LoggingExtensions()
        {
            _remoteAuthenticationError = LoggerMessage.Define<string>(
                eventId: 4,
                logLevel: LogLevel.Information,
                formatString: "Error from RemoteAuthentication: {ErrorMessage}.");
            _signInHandled = LoggerMessage.Define(
                eventId: 5,
                logLevel: LogLevel.Debug,
                formatString: "The SigningIn event returned Handled.");
            _signInSkipped = LoggerMessage.Define(
                eventId: 6,
                logLevel: LogLevel.Debug,
                formatString: "The SigningIn event returned Skipped.");
            _authSchemeNotAuthenticatedWithFailure = LoggerMessage.Define<string, string>(
                eventId: 7,
                logLevel: LogLevel.Information,
                formatString: "{AuthenticationScheme} was not authenticated. Failure message: {FailureMessage}");
            _authSchemeAuthenticated = LoggerMessage.Define<string>(
                eventId: 8,
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was successfully authenticated.");
            _authSchemeNotAuthenticated = LoggerMessage.Define<string>(
                eventId: 9,
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was not authenticated.");
            _authSchemeChallenged = LoggerMessage.Define<string>(
                eventId: 12,
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was challenged.");
            _authSchemeForbidden = LoggerMessage.Define<string>(
                eventId: 13,
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was forbidden.");
            _correlationPropertyNotFound = LoggerMessage.Define<string>(
                eventId: 14,
                logLevel: LogLevel.Warning,
                formatString: "{CorrelationProperty} state property not found.");
            _correlationCookieNotFound = LoggerMessage.Define<string>(
                eventId: 15,
                logLevel: LogLevel.Warning,
                formatString: "'{CorrelationCookieName}' cookie not found.");
            _unexpectedCorrelationCookieValue = LoggerMessage.Define<string, string>(
               eventId: 16,
               logLevel: LogLevel.Warning,
               formatString: "The correlation cookie value '{CorrelationCookieName}' did not match the expected value '{CorrelationCookieValue}'.");
            _accessDeniedError = LoggerMessage.Define(
                eventId: 17,
                logLevel: LogLevel.Information,
                formatString: "Access was denied by the resource owner or by the remote server.");
            _accessDeniedContextHandled = LoggerMessage.Define(
                eventId: 18,
                logLevel: LogLevel.Debug,
                formatString: "The AccessDenied event returned Handled.");
            _accessDeniedContextSkipped = LoggerMessage.Define(
                eventId: 19,
                logLevel: LogLevel.Debug,
                formatString: "The AccessDenied event returned Skipped.");
        }

        public static void AuthenticationSchemeAuthenticated(this ILogger logger, string authenticationScheme)
        {
            _authSchemeAuthenticated(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeNotAuthenticated(this ILogger logger, string authenticationScheme)
        {
            _authSchemeNotAuthenticated(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeNotAuthenticatedWithFailure(this ILogger logger, string authenticationScheme, string failureMessage)
        {
            _authSchemeNotAuthenticatedWithFailure(logger, authenticationScheme, failureMessage, null);
        }

        public static void AuthenticationSchemeChallenged(this ILogger logger, string authenticationScheme)
        {
            _authSchemeChallenged(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeForbidden(this ILogger logger, string authenticationScheme)
        {
            _authSchemeForbidden(logger, authenticationScheme, null);
        }

        public static void RemoteAuthenticationError(this ILogger logger, string errorMessage)
        {
            _remoteAuthenticationError(logger, errorMessage, null);
        }

        public static void SigninHandled(this ILogger logger)
        {
            _signInHandled(logger, null);
        }

        public static void SigninSkipped(this ILogger logger)
        {
            _signInSkipped(logger, null);
        }

        public static void CorrelationPropertyNotFound(this ILogger logger, string correlationPrefix)
        {
            _correlationPropertyNotFound(logger, correlationPrefix, null);
        }

        public static void CorrelationCookieNotFound(this ILogger logger, string cookieName)
        {
            _correlationCookieNotFound(logger, cookieName, null);
        }

        public static void UnexpectedCorrelationCookieValue(this ILogger logger, string cookieName, string cookieValue)
        {
            _unexpectedCorrelationCookieValue(logger, cookieName, cookieValue, null);
        }

        public static void AccessDeniedError(this ILogger logger)
        {
            _accessDeniedError(logger, null);
        }

        public static void AccessDeniedContextHandled(this ILogger logger)
        {
            _accessDeniedContextHandled(logger, null);
        }

        public static void AccessDeniedContextSkipped(this ILogger logger)
        {
            _accessDeniedContextSkipped(logger, null);
        }
    }
}

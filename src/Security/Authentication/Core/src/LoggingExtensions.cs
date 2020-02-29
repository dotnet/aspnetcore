// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _authenticationSchemeAuthenticated;
        private static readonly Action<ILogger, string, Exception> _authenticationSchemeNotAuthenticated;
        private static readonly Action<ILogger, string, string, Exception> _authenticationSchemeNotAuthenticatedWithFailure;
        private static readonly Action<ILogger, string, Exception> _authenticationSchemeChallenged;
        private static readonly Action<ILogger, string, Exception> _authenticationSchemeForbidden;
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
                eventId: new EventId(4, "RemoteAuthenticationFailed"),
                logLevel: LogLevel.Information,
                formatString: "Error from RemoteAuthentication: {ErrorMessage}.");
            _signInHandled = LoggerMessage.Define(
                eventId: new EventId(5, "SignInHandled"),
                logLevel: LogLevel.Debug,
                formatString: "The SigningIn event returned Handled.");
            _signInSkipped = LoggerMessage.Define(
                eventId: new EventId(6, "SignInSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "The SigningIn event returned Skipped.");
            _authenticationSchemeNotAuthenticatedWithFailure = LoggerMessage.Define<string, string>(
                eventId: new EventId(7, "AuthenticationSchemeNotAuthenticatedWithFailure"),
                logLevel: LogLevel.Information,
                formatString: "{AuthenticationScheme} was not authenticated. Failure message: {FailureMessage}");
            _authenticationSchemeAuthenticated = LoggerMessage.Define<string>(
                eventId: new EventId(8, "AuthenticationSchemeAuthenticated"),
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was successfully authenticated.");
            _authenticationSchemeNotAuthenticated = LoggerMessage.Define<string>(
                eventId: new EventId(9, "AuthenticationSchemeNotAuthenticated"),
                logLevel: LogLevel.Debug,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was not authenticated.");
            _authenticationSchemeChallenged = LoggerMessage.Define<string>(
                eventId: new EventId(12, "AuthenticationSchemeChallenged"),
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was challenged.");
            _authenticationSchemeForbidden = LoggerMessage.Define<string>(
                eventId: new EventId(13, "AuthenticationSchemeForbidden"),
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} was forbidden.");
            _correlationPropertyNotFound = LoggerMessage.Define<string>(
                eventId: new EventId(14, "CorrelationPropertyNotFound"),
                logLevel: LogLevel.Warning,
                formatString: "{CorrelationProperty} state property not found.");
            _correlationCookieNotFound = LoggerMessage.Define<string>(
                eventId: new EventId(15, "CorrelationCookieNotFound"),
                logLevel: LogLevel.Warning,
                formatString: "'{CorrelationCookieName}' cookie not found.");
            _unexpectedCorrelationCookieValue = LoggerMessage.Define<string, string>(
                eventId: new EventId(16, "UnexpectedCorrelationCookieValue"),
               logLevel: LogLevel.Warning,
               formatString: "The correlation cookie value '{CorrelationCookieName}' did not match the expected value '{CorrelationCookieValue}'.");
            _accessDeniedError = LoggerMessage.Define(
                eventId: new EventId(17, "AccessDenied"),
                logLevel: LogLevel.Information,
                formatString: "Access was denied by the resource owner or by the remote server.");
            _accessDeniedContextHandled = LoggerMessage.Define(
                eventId: new EventId(18, "AccessDeniedContextHandled"),
                logLevel: LogLevel.Debug,
                formatString: "The AccessDenied event returned Handled.");
            _accessDeniedContextSkipped = LoggerMessage.Define(
                eventId: new EventId(19, "AccessDeniedContextSkipped"),
                logLevel: LogLevel.Debug,
                formatString: "The AccessDenied event returned Skipped.");
        }

        public static void AuthenticationSchemeAuthenticated(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeAuthenticated(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeNotAuthenticated(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeNotAuthenticated(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeNotAuthenticatedWithFailure(this ILogger logger, string authenticationScheme, string failureMessage)
        {
            _authenticationSchemeNotAuthenticatedWithFailure(logger, authenticationScheme, failureMessage, null);
        }

        public static void AuthenticationSchemeChallenged(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeChallenged(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeForbidden(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeForbidden(logger, authenticationScheme, null);
        }

        public static void RemoteAuthenticationError(this ILogger logger, string errorMessage)
        {
            _remoteAuthenticationError(logger, errorMessage, null);
        }

        public static void SignInHandled(this ILogger logger)
        {
            _signInHandled(logger, null);
        }

        public static void SignInSkipped(this ILogger logger)
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

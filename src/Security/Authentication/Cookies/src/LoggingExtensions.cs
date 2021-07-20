// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception?> _authenticationSchemeSignedIn = LoggerMessage.Define<string>(
                eventId: new EventId(10, "AuthenticationSchemeSignedIn"),
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} signed in.");
        private static readonly Action<ILogger, string, Exception?> _authenticationSchemeSignedOut = LoggerMessage.Define<string>(
                eventId: new EventId(11, "AuthenticationSchemeSignedOut"),
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} signed out.");

        public static void AuthenticationSchemeSignedIn(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeSignedIn(logger, authenticationScheme, null);
        }

        public static void AuthenticationSchemeSignedOut(this ILogger logger, string authenticationScheme)
        {
            _authenticationSchemeSignedOut(logger, authenticationScheme, null);
        }
    }
}

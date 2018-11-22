// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, string, Exception> _authSchemeSignedIn;
        private static Action<ILogger, string, Exception> _authSchemeSignedOut;

        static LoggingExtensions()
        {
            _authSchemeSignedIn = LoggerMessage.Define<string>(
                eventId: 10,
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} signed in.");
            _authSchemeSignedOut = LoggerMessage.Define<string>(
                eventId: 11,
                logLevel: LogLevel.Information,
                formatString: "AuthenticationScheme: {AuthenticationScheme} signed out.");
        }

        public static void SignedIn(this ILogger logger, string authenticationScheme)
        {
            _authSchemeSignedIn(logger, authenticationScheme, null);
        }

        public static void SignedOut(this ILogger logger, string authenticationScheme)
        {
            _authSchemeSignedOut(logger, authenticationScheme, null);
        }
    }
}

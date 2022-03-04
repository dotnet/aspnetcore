// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _userAuthorizationFailed;
        private static Action<ILogger, Exception> _userAuthorizationSucceeded;

        static LoggingExtensions()
        {
            _userAuthorizationSucceeded = LoggerMessage.Define(
                eventId: new EventId(1, "UserAuthorizationSucceeded"),
                logLevel: LogLevel.Information,
                formatString: "Authorization was successful.");
            _userAuthorizationFailed = LoggerMessage.Define(
                eventId: new EventId(2, "UserAuthorizationFailed"),
                logLevel: LogLevel.Information,
                formatString: "Authorization failed.");
        }

        public static void UserAuthorizationSucceeded(this ILogger logger)
            => _userAuthorizationSucceeded(logger, null);

        public static void UserAuthorizationFailed(this ILogger logger)
            => _userAuthorizationFailed(logger, null);
    }
}

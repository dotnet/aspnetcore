// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, string, Exception> _userAuthorizationFailed;
        private static Action<ILogger, string, Exception> _userAuthorizationSucceeded;

        static LoggingExtensions()
        {
            _userAuthorizationSucceeded = LoggerMessage.Define<string>(
                eventId: 1,
                logLevel: LogLevel.Information,
                formatString: "Authorization was successful for user: {UserName}.");
            _userAuthorizationFailed = LoggerMessage.Define<string>(
                eventId: 2,
                logLevel: LogLevel.Information,
                formatString: "Authorization failed for user: {UserName}.");
        }

        public static void UserAuthorizationSucceeded(this ILogger logger, string userName)
        {
            _userAuthorizationSucceeded(logger, userName, null);
        }

        public static void UserAuthorizationFailed(this ILogger logger, string userName)
        {
            _userAuthorizationFailed(logger, userName, null);
        }
    }
}

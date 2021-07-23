// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception?> _userAuthorizationFailed = LoggerMessage.Define<string>(
            eventId: new EventId(2, "UserAuthorizationFailed"),
            logLevel: LogLevel.Information,
            formatString: "Authorization failed. {0}");

        private static readonly Action<ILogger, Exception?> _userAuthorizationSucceeded = LoggerMessage.Define(
            eventId: new EventId(1, "UserAuthorizationSucceeded"),
            logLevel: LogLevel.Debug,
            formatString: "Authorization was successful.");

        public static void UserAuthorizationSucceeded(this ILogger logger)
            => _userAuthorizationSucceeded(logger, null);

        public static void UserAuthorizationFailed(this ILogger logger, AuthorizationFailure failure)
        {
            var reason = failure.FailCalled
                ? "Fail() was explicitly called."
                : "These requirements were not met:" + Environment.NewLine + string.Join(Environment.NewLine, failure.FailedRequirements);

            _userAuthorizationFailed(logger, reason, null);
        }
    }
}

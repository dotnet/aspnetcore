// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, string, string, Exception> _handleChallenge;

        static LoggingExtensions()
        {
            _handleChallenge = LoggerMessage.Define<string, string>(
                eventId: new EventId(1, "HandleChallenge"),
                logLevel: LogLevel.Debug,
                formatString: "HandleChallenge with Location: {Location}; and Set-Cookie: {Cookie}.");
        }

        public static void HandleChallenge(this ILogger logger, string location, string cookie)
            => _handleChallenge(logger, location, cookie, null);
    }
}

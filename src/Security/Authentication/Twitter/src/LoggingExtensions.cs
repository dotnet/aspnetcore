// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _obtainRequestToken;
        private static Action<ILogger, Exception> _obtainAccessToken;
        private static Action<ILogger, Exception> _retrieveUserDetails;

        static LoggingExtensions()
        {
            _obtainRequestToken = LoggerMessage.Define(
                eventId: 1,
                logLevel: LogLevel.Debug,
                formatString: "ObtainRequestToken");
            _obtainAccessToken = LoggerMessage.Define(
                eventId: 2,
                logLevel: LogLevel.Debug,
                formatString: "ObtainAccessToken");
            _retrieveUserDetails = LoggerMessage.Define(
                eventId: 3,
                logLevel: LogLevel.Debug,
                formatString: "RetrieveUserDetails");

        }

        public static void ObtainAccessToken(this ILogger logger)
        {
            _obtainAccessToken(logger, null);
        }

        public static void ObtainRequestToken(this ILogger logger)
        {
            _obtainRequestToken(logger, null);
        }

        public static void RetrieveUserDetails(this ILogger logger)
        {
            _retrieveUserDetails(logger, null);
        }
    }
}

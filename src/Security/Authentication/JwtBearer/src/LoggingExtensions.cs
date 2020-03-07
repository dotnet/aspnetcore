// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _tokenValidationFailed;
        private static Action<ILogger, Exception> _tokenValidationSucceeded;
        private static Action<ILogger, Exception> _errorProcessingMessage;

        static LoggingExtensions()
        {
            _tokenValidationFailed = LoggerMessage.Define(
                eventId: new EventId(1, "TokenValidationFailed"),
                logLevel: LogLevel.Information,
                formatString: "Failed to validate the token.");
            _tokenValidationSucceeded = LoggerMessage.Define(
                eventId: new EventId(2, "TokenValidationSucceeded"),
                logLevel: LogLevel.Information,
                formatString: "Successfully validated the token.");
            _errorProcessingMessage = LoggerMessage.Define(
                eventId: new EventId(3, "ProcessingMessageFailed"),
                logLevel: LogLevel.Error,
                formatString: "Exception occurred while processing message.");
        }

        public static void TokenValidationFailed(this ILogger logger, Exception ex)
            => _tokenValidationFailed(logger, ex);

        public static void TokenValidationSucceeded(this ILogger logger)
            => _tokenValidationSucceeded(logger, null);

        public static void ErrorProcessingMessage(this ILogger logger, Exception ex)
            => _errorProcessingMessage(logger, ex);
    }
}

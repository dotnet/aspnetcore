// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{ 
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _invalidExpirationTime;
        private static readonly Action<ILogger, Exception> _userIdsNotEquals;
        private static readonly Action<ILogger, string, string, Exception> _purposeNotEquals;
        private static readonly Action<ILogger, Exception> _unexpectedEndOfInput;
        private static readonly Action<ILogger, Exception> _securityStampNotEquals;
        private static readonly Action<ILogger, Exception> _securityStampIsNotEmpty;
        private static readonly Action<ILogger, Exception> _unhandledException;

        static LoggingExtensions()
        {
            _invalidExpirationTime = LoggerMessage.Define(
                eventId: new EventId(0, "InvalidExpirationTime"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: the expiration time is invalid.");

            _userIdsNotEquals = LoggerMessage.Define(
                eventId: new EventId(1, "UserIdsNotEquals"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: did not find expected UserId.");

            _purposeNotEquals = LoggerMessage.Define<string, string>(
                eventId: new EventId(2, "PurposeNotEquals"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: did not find expected purpose. '{ActualPurpose}' does not match the expected purpose '{ExpectedPurpose}'.");

            _unexpectedEndOfInput = LoggerMessage.Define(
                eventId: new EventId(3, "UnexpectedEndOfInput"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: unexpected end of input.");

            _securityStampNotEquals = LoggerMessage.Define(
                eventId: new EventId(4, "SecurityStampNotEquals"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: did not find expected security stamp.");

            _securityStampIsNotEmpty = LoggerMessage.Define(
                eventId: new EventId(5, "SecurityStampIsNotEmpty"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: the expected stamp is not empty.");

            _unhandledException = LoggerMessage.Define(
                eventId: new EventId(6, "UnhandledException"),
                logLevel: LogLevel.Debug,
                formatString: "ValidateAsync failed: unhandled exception was thrown.");
        }

        public static void InvalidExpirationTime(this ILogger logger)
        {
            _invalidExpirationTime(logger, null);
        }

        public static void UserIdsNotEquals(this ILogger logger)
        {
            _userIdsNotEquals(logger, null);
        }

        public static void PurposeNotEquals(this ILogger logger, string actualPurpose, string expectedPurpose)
        {
            _purposeNotEquals(logger, actualPurpose, expectedPurpose,  null);
        }

        public static void UnexpectedEndOfInput(this ILogger logger)
        {
            _unexpectedEndOfInput(logger, null);
        }

        public static void SequrityStampNotEquals(this ILogger logger)
        {
            _securityStampNotEquals(logger, null);
        }

        public static void SecurityStampIsNotEmpty(this ILogger logger)
        {
            _securityStampIsNotEmpty(logger, null);
        }

        public static void UnhandledException(this ILogger logger)
        {
            _unhandledException(logger, null);
        }
    }
}

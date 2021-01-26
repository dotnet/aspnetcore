// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _failedToReadStackFrameInfo;

        static LoggerExtensions()
        {
            _failedToReadStackFrameInfo = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(0, "FailedToReadStackTraceInfo"),
                formatString: "Failed to read stack trace information for exception.");
        }

        public static void FailedToReadStackTraceInfo(this ILogger logger, Exception exception)
        {
            _failedToReadStackFrameInfo(logger, exception);
        }
    }
}

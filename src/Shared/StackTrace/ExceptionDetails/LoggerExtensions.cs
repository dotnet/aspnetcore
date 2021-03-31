// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

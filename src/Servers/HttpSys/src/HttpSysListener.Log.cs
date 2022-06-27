// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class HttpSysListener
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.ListenerDisposeError, LogLevel.Error, "Dispose", EventName = "ListenerDisposeError")]
        public static partial void ListenerDisposeError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ListenerDisposing, LogLevel.Trace, "Disposing the listener.", EventName = "ListenerDisposing")]
        public static partial void ListenerDisposing(ILogger logger);

        [LoggerMessage(LoggerEventIds.HttpSysListenerCtorError, LogLevel.Error, ".Ctor", EventName = "HttpSysListenerCtorError")]
        public static partial void HttpSysListenerCtorError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ListenerStartError, LogLevel.Error, "Start", EventName = "ListenerStartError")]
        public static partial void ListenerStartError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ListenerStarting, LogLevel.Trace, "Starting the listener.", EventName = "ListenerStarting")]
        public static partial void ListenerStarting(ILogger logger);

        [LoggerMessage(LoggerEventIds.ListenerStopError, LogLevel.Error, "Stop", EventName = "ListenerStopError")]
        public static partial void ListenerStopError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ListenerStopping, LogLevel.Trace, "Stopping the listener.", EventName = "ListenerStopping")]
        public static partial void ListenerStopping(ILogger logger);

        [LoggerMessage(LoggerEventIds.RequestValidationFailed, LogLevel.Error, "Error validating request {RequestId}", EventName = "RequestValidationFailed")]
        public static partial void RequestValidationFailed(ILogger logger, Exception exception, ulong requestId);
    }
}

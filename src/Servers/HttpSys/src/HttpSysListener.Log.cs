// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class HttpSysListener
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _listenerDisposeError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ListenerDisposeError, "Dispose");

            private static readonly Action<ILogger, Exception?> _listenerDisposing =
                LoggerMessage.Define(LogLevel.Trace, LoggerEventIds.ListenerDisposing, "Disposing the listener.");

            private static readonly Action<ILogger, Exception?> _httpSysListenerCtorError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.HttpSysListenerCtorError, ".Ctor");

            private static readonly Action<ILogger, Exception?> _listenerStartError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ListenerStartError, "Start");

            private static readonly Action<ILogger, Exception?> _listenerStarting =
                LoggerMessage.Define(LogLevel.Trace, LoggerEventIds.ListenerStarting, "Starting the listener.");

            private static readonly Action<ILogger, Exception?> _listenerStopError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ListenerStopError, "Stop");

            private static readonly Action<ILogger, Exception?> _listenerStopping =
                LoggerMessage.Define(LogLevel.Trace, LoggerEventIds.ListenerStopping, "Stopping the listener.");

            private static readonly Action<ILogger, ulong, Exception?> _requestValidationFailed =
                LoggerMessage.Define<ulong>(LogLevel.Error, LoggerEventIds.RequestValidationFailed, "Error validating request {RequestId}");

            public static void ListenerDisposeError(ILogger logger, Exception exception)
            {
                _listenerDisposeError(logger, exception);
            }

            public static void ListenerDisposing(ILogger logger)
            {
                _listenerDisposing(logger, null);
            }

            public static void HttpSysListenerCtorError(ILogger logger, Exception exception)
            {
                _httpSysListenerCtorError(logger, exception);
            }

            public static void ListenerStartError(ILogger logger, Exception exception)
            {
                _listenerStartError(logger, exception);
            }

            public static void ListenerStarting(ILogger logger)
            {
                _listenerStarting(logger, null);
            }

            public static void ListenerStopError(ILogger logger, Exception exception)
            {
                _listenerStopError(logger, exception);
            }

            public static void ListenerStopping(ILogger logger)
            {
                _listenerStopping(logger, null);
            }

            public static void RequestValidationFailed(ILogger logger, Exception exception, ulong requestId)
            {
                _requestValidationFailed(logger, requestId, exception);
            }
        }
    }
}

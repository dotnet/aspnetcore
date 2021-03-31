// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    internal static class HubConnectionHandlerLog
    {
        private static readonly Action<ILogger, string, Exception?> _errorDispatchingHubEvent =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "ErrorDispatchingHubEvent"), "Error when dispatching '{HubMethod}' on hub.");

        private static readonly Action<ILogger, Exception?> _errorProcessingRequest =
            LoggerMessage.Define(LogLevel.Debug, new EventId(2, "ErrorProcessingRequest"), "Error when processing requests.");

        private static readonly Action<ILogger, Exception?> _abortFailed =
            LoggerMessage.Define(LogLevel.Trace, new EventId(3, "AbortFailed"), "Abort callback failed.");

        private static readonly Action<ILogger, Exception?> _errorSendingClose =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ErrorSendingClose"), "Error when sending Close message.");

        private static readonly Action<ILogger, Exception?> _connectedStarting =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ConnectedStarting"), "OnConnectedAsync started.");

        private static readonly Action<ILogger, Exception?> _connectedEnding =
            LoggerMessage.Define(LogLevel.Debug, new EventId(6, "ConnectedEnding"), "OnConnectedAsync ending.");

        public static void ErrorDispatchingHubEvent(ILogger logger, string hubMethod, Exception exception)
        {
            _errorDispatchingHubEvent(logger, hubMethod, exception);
        }

        public static void ErrorProcessingRequest(ILogger logger, Exception exception)
        {
            _errorProcessingRequest(logger, exception);
        }

        public static void AbortFailed(ILogger logger, Exception exception)
        {
            _abortFailed(logger, exception);
        }

        public static void ErrorSendingClose(ILogger logger, Exception exception)
        {
            _errorSendingClose(logger, exception);
        }

        public static void ConnectedStarting(ILogger logger)
        {
            _connectedStarting(logger, null);
        }

        public static void ConnectedEnding(ILogger logger)
        {
            _connectedEnding(logger, null);
        }
    }
}

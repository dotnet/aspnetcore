// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public partial class LongPollingTransport
    {
        private static class Log
        {
            private static readonly Action<ILogger, TransferMode, Exception> _startTransport =
                LoggerMessage.Define<TransferMode>(LogLevel.Information, new EventId(1, "StartTransport"), "Starting transport. Transfer mode: {transferMode}.");

            private static readonly Action<ILogger, Exception> _transportStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "TransportStopped"), "Transport stopped.");

            private static readonly Action<ILogger, Exception> _startReceive =
                LoggerMessage.Define(LogLevel.Debug, new EventId(3, "StartReceive"), "Starting receive loop.");

            private static readonly Action<ILogger, Exception> _receiveStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ReceiveStopped"), "Receive loop stopped.");

            private static readonly Action<ILogger, Exception> _receiveCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ReceiveCanceled"), "Receive loop canceled.");

            private static readonly Action<ILogger, Exception> _transportStopping =
                LoggerMessage.Define(LogLevel.Information, new EventId(6, "TransportStopping"), "Transport is stopping.");

            private static readonly Action<ILogger, Exception> _closingConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(7, "ClosingConnection"), "The server is closing the connection.");

            private static readonly Action<ILogger, Exception> _receivedMessages =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "ReceivedMessages"), "Received messages from the server.");

            private static readonly Action<ILogger, Uri, Exception> _errorPolling =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(9, "ErrorPolling"), "Error while polling '{pollUrl}'.");

            // EventIds 100 - 106 used in SendUtils

            public static void StartTransport(ILogger logger, TransferMode transferMode)
            {
                _startTransport(logger, transferMode, null);
            }

            public static void TransportStopped(ILogger logger, Exception exception)
            {
                _transportStopped(logger, exception);
            }

            public static void StartReceive(ILogger logger)
            {
                _startReceive(logger, null);
            }

            public static void TransportStopping(ILogger logger)
            {
                _transportStopping(logger, null);
            }

            public static void ReceiveCanceled(ILogger logger)
            {
                _receiveCanceled(logger, null);
            }

            public static void ReceiveStopped(ILogger logger)
            {
                _receiveStopped(logger, null);
            }

            public static void ClosingConnection(ILogger logger)
            {
                _closingConnection(logger, null);
            }

            public static void ReceivedMessages(ILogger logger)
            {
                _receivedMessages(logger, null);
            }

            public static void ErrorPolling(ILogger logger, Uri pollUrl, Exception exception)
            {
                _errorPolling(logger, pollUrl, exception);
            }
        }
    }
}

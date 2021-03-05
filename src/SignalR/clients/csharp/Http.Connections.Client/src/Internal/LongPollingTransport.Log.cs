// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal partial class LongPollingTransport
    {
        private static class Log
        {
            private static readonly Action<ILogger, TransferFormat, Exception?> _startTransport =
                LoggerMessage.Define<TransferFormat>(LogLevel.Information, new EventId(1, "StartTransport"), "Starting transport. Transfer mode: {TransferFormat}.");

            private static readonly Action<ILogger, Exception?> _transportStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "TransportStopped"), "Transport stopped.");

            private static readonly Action<ILogger, Exception?> _startReceive =
                LoggerMessage.Define(LogLevel.Debug, new EventId(3, "StartReceive"), "Starting receive loop.");

            private static readonly Action<ILogger, Exception?> _receiveStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ReceiveStopped"), "Receive loop stopped.");

            private static readonly Action<ILogger, Exception?> _receiveCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ReceiveCanceled"), "Receive loop canceled.");

            private static readonly Action<ILogger, Exception?> _transportStopping =
                LoggerMessage.Define(LogLevel.Information, new EventId(6, "TransportStopping"), "Transport is stopping.");

            private static readonly Action<ILogger, Exception?> _closingConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(7, "ClosingConnection"), "The server is closing the connection.");

            private static readonly Action<ILogger, Exception?> _receivedMessages =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "ReceivedMessages"), "Received messages from the server.");

            private static readonly Action<ILogger, Uri, Exception> _errorPolling =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(9, "ErrorPolling"), "Error while polling '{PollUrl}'.");

            // long? does properly format as "(null)" when null.
            private static readonly Action<ILogger, int, long?, Exception?> _pollResponseReceived =
                LoggerMessage.Define<int, long?>(LogLevel.Trace, new EventId(10, "PollResponseReceived"),
                    "Poll response with status code {StatusCode} received from server. Content length: {ContentLength}.");

            private static readonly Action<ILogger, Uri, Exception?> _sendingDeleteRequest =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(11, "SendingDeleteRequest"), "Sending DELETE request to '{PollUrl}'.");

            private static readonly Action<ILogger, Uri, Exception?> _deleteRequestAccepted =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(12, "DeleteRequestAccepted"), "DELETE request to '{PollUrl}' accepted.");

            private static readonly Action<ILogger, Uri, Exception> _errorSendingDeleteRequest =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(13, "ErrorSendingDeleteRequest"), "Error sending DELETE request to '{PollUrl}'.");

            private static readonly Action<ILogger, Uri, Exception?> _connectionAlreadyClosedSendingDeleteRequest =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(14, "ConnectionAlreadyClosedSendingDeleteRequest"), "A 404 response was returned from sending DELETE request to '{PollUrl}', likely because the transport was already closed on the server.");

            // EventIds 100 - 106 used in SendUtils

            public static void StartTransport(ILogger logger, TransferFormat transferFormat)
            {
                _startTransport(logger, transferFormat, null);
            }

            public static void TransportStopped(ILogger logger, Exception? exception)
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

            public static void PollResponseReceived(ILogger logger, HttpResponseMessage response)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _pollResponseReceived(logger, (int)response.StatusCode,
                        response.Content.Headers.ContentLength ?? -1, null);
                }
            }

            public static void SendingDeleteRequest(ILogger logger, Uri pollUrl)
            {
                _sendingDeleteRequest(logger, pollUrl, null);
            }

            public static void DeleteRequestAccepted(ILogger logger, Uri pollUrl)
            {
                _deleteRequestAccepted(logger, pollUrl, null);
            }

            public static void ErrorSendingDeleteRequest(ILogger logger, Uri pollUrl, Exception ex)
            {
                _errorSendingDeleteRequest(logger, pollUrl, ex);
            }

            public static void ConnectionAlreadyClosedSendingDeleteRequest(ILogger logger, Uri pollUrl)
            {
                _connectionAlreadyClosedSendingDeleteRequest(logger, pollUrl, null);
            }
        }
    }
}

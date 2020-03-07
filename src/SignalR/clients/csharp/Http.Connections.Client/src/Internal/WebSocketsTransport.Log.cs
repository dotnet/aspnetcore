// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal partial class WebSocketsTransport
    {
        private static class Log
        {
            private static readonly Action<ILogger, TransferFormat, Uri, Exception> _startTransport =
                LoggerMessage.Define<TransferFormat, Uri>(LogLevel.Information, new EventId(1, "StartTransport"), "Starting transport. Transfer mode: {TransferFormat}. Url: '{WebSocketUrl}'.");

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

            private static readonly Action<ILogger, Exception> _sendStarted =
                LoggerMessage.Define(LogLevel.Debug, new EventId(7, "SendStarted"), "Starting the send loop.");

            private static readonly Action<ILogger, Exception> _sendStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "SendStopped"), "Send loop stopped.");

            private static readonly Action<ILogger, Exception> _sendCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(9, "SendCanceled"), "Send loop canceled.");

            private static readonly Action<ILogger, int, Exception> _messageToApp =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(10, "MessageToApp"), "Passing message to application. Payload size: {Count}.");

            private static readonly Action<ILogger, WebSocketCloseStatus?, Exception> _webSocketClosed =
                LoggerMessage.Define<WebSocketCloseStatus?>(LogLevel.Information, new EventId(11, "WebSocketClosed"), "WebSocket closed by the server. Close status {CloseStatus}.");

            private static readonly Action<ILogger, WebSocketMessageType, int, bool, Exception> _messageReceived =
                LoggerMessage.Define<WebSocketMessageType, int, bool>(LogLevel.Debug, new EventId(12, "MessageReceived"), "Message received. Type: {MessageType}, size: {Count}, EndOfMessage: {EndOfMessage}.");

            private static readonly Action<ILogger, long, Exception> _receivedFromApp =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(13, "ReceivedFromApp"), "Received message from application. Payload size: {Count}.");

            private static readonly Action<ILogger, Exception> _sendMessageCanceled =
                LoggerMessage.Define(LogLevel.Information, new EventId(14, "SendMessageCanceled"), "Sending a message canceled.");

            private static readonly Action<ILogger, Exception> _errorSendingMessage =
                LoggerMessage.Define(LogLevel.Error, new EventId(15, "ErrorSendingMessage"), "Error while sending a message.");

            private static readonly Action<ILogger, Exception> _closingWebSocket =
                LoggerMessage.Define(LogLevel.Information, new EventId(16, "ClosingWebSocket"), "Closing WebSocket.");

            private static readonly Action<ILogger, Exception> _closingWebSocketFailed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(17, "ClosingWebSocketFailed"), "Closing webSocket failed.");

            private static readonly Action<ILogger, Exception> _cancelMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(18, "CancelMessage"), "Canceled passing message to application.");

            private static readonly Action<ILogger, Exception> _startedTransport =
                LoggerMessage.Define(LogLevel.Debug, new EventId(19, "StartedTransport"), "Started transport.");

            public static void StartTransport(ILogger logger, TransferFormat transferFormat, Uri webSocketUrl)
            {
                _startTransport(logger, transferFormat, webSocketUrl, null);
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

            public static void MessageToApp(ILogger logger, int count)
            {
                _messageToApp(logger, count, null);
            }

            public static void ReceiveCanceled(ILogger logger)
            {
                _receiveCanceled(logger, null);
            }

            public static void ReceiveStopped(ILogger logger)
            {
                _receiveStopped(logger, null);
            }

            public static void SendStarted(ILogger logger)
            {
                _sendStarted(logger, null);
            }

            public static void SendCanceled(ILogger logger)
            {
                _sendCanceled(logger, null);
            }

            public static void SendStopped(ILogger logger)
            {
                _sendStopped(logger, null);
            }

            public static void WebSocketClosed(ILogger logger, WebSocketCloseStatus? closeStatus)
            {
                _webSocketClosed(logger, closeStatus, null);
            }

            public static void MessageReceived(ILogger logger, WebSocketMessageType messageType, int count, bool endOfMessage)
            {
                _messageReceived(logger, messageType, count, endOfMessage, null);
            }

            public static void ReceivedFromApp(ILogger logger, long count)
            {
                _receivedFromApp(logger, count, null);
            }

            public static void SendMessageCanceled(ILogger logger)
            {
                _sendMessageCanceled(logger, null);
            }

            public static void ErrorSendingMessage(ILogger logger, Exception exception)
            {
                _errorSendingMessage(logger, exception);
            }

            public static void ClosingWebSocket(ILogger logger)
            {
                _closingWebSocket(logger, null);
            }

            public static void ClosingWebSocketFailed(ILogger logger, Exception exception)
            {
                _closingWebSocketFailed(logger, exception);
            }

            public static void CancelMessage(ILogger logger)
            {
                _cancelMessage(logger, null);
            }

            public static void StartedTransport(ILogger logger)
            {
                _startedTransport(logger, null);
            }
        }
    }
}

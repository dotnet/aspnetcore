// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal.Transports
{
    public partial class WebSocketsTransport
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception> _socketOpened =
                LoggerMessage.Define(LogLevel.Debug, new EventId(1, "SocketOpened"), "Socket opened.");

            private static readonly Action<ILogger, Exception> _socketClosed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "SocketClosed"), "Socket closed.");

            private static readonly Action<ILogger, WebSocketCloseStatus?, string, Exception> _clientClosed =
                LoggerMessage.Define<WebSocketCloseStatus?, string>(LogLevel.Debug, new EventId(3, "ClientClosed"), "Client closed connection with status code '{status}' ({description}). Signaling end-of-input to application.");

            private static readonly Action<ILogger, Exception> _waitingForSend =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "WaitingForSend"), "Waiting for the application to finish sending data.");

            private static readonly Action<ILogger, Exception> _failedSending =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "FailedSending"), "Application failed during sending. Sending InternalServerError close frame.");

            private static readonly Action<ILogger, Exception> _finishedSending =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "FinishedSending"), "Application finished sending. Sending close frame.");

            private static readonly Action<ILogger, Exception> _waitingForClose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(7, "WaitingForClose"), "Waiting for the client to close the socket.");

            private static readonly Action<ILogger, Exception> _closeTimedOut =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "CloseTimedOut"), "Timed out waiting for client to send the close frame, aborting the connection.");

            private static readonly Action<ILogger, WebSocketMessageType, int, bool, Exception> _messageReceived =
                LoggerMessage.Define<WebSocketMessageType, int, bool>(LogLevel.Trace, new EventId(9, "MessageReceived"), "Message received. Type: {messageType}, size: {size}, EndOfMessage: {endOfMessage}.");

            private static readonly Action<ILogger, int, Exception> _messageToApplication =
                LoggerMessage.Define<int>(LogLevel.Trace, new EventId(10, "MessageToApplication"), "Passing message to application. Payload size: {size}.");

            private static readonly Action<ILogger, long, Exception> _sendPayload =
                LoggerMessage.Define<long>(LogLevel.Trace, new EventId(11, "SendPayload"), "Sending payload: {size} bytes.");

            private static readonly Action<ILogger, Exception> _errorWritingFrame =
                LoggerMessage.Define(LogLevel.Error, new EventId(12, "ErrorWritingFrame"), "Error writing frame.");

            private static readonly Action<ILogger, Exception> _sendFailed =
                LoggerMessage.Define(LogLevel.Error, new EventId(13, "SendFailed"), "Socket failed to send.");

            public static void SocketOpened(ILogger logger)
            {
                _socketOpened(logger, null);
            }

            public static void SocketClosed(ILogger logger)
            {
                _socketClosed(logger, null);
            }

            public static void ClientClosed(ILogger logger, WebSocketCloseStatus? closeStatus, string closeDescription)
            {
                _clientClosed(logger, closeStatus, closeDescription, null);
            }

            public static void WaitingForSend(ILogger logger)
            {
                _waitingForSend(logger, null);
            }

            public static void FailedSending(ILogger logger)
            {
                _failedSending(logger, null);
            }

            public static void FinishedSending(ILogger logger)
            {
                _finishedSending(logger, null);
            }

            public static void WaitingForClose(ILogger logger)
            {
                _waitingForClose(logger, null);
            }

            public static void CloseTimedOut(ILogger logger)
            {
                _closeTimedOut(logger, null);
            }

            public static void MessageReceived(ILogger logger, WebSocketMessageType type, int size, bool endOfMessage)
            {
                _messageReceived(logger, type, size, endOfMessage, null);
            }

            public static void MessageToApplication(ILogger logger, int size)
            {
                _messageToApplication(logger, size, null);
            }

            public static void SendPayload(ILogger logger, long size)
            {
                _sendPayload(logger, size, null);
            }

            public static void ErrorWritingFrame(ILogger logger, Exception ex)
            {
                _errorWritingFrame(logger, ex);
            }

            public static void SendFailed(ILogger logger, Exception ex)
            {
                _sendFailed(logger, ex);
            }

        }
    }
}

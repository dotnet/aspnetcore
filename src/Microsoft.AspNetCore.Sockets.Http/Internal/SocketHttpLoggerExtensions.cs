// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    internal static class SocketHttpLoggerExtensions
    {
        // Category: LongPollingTransport
        private static readonly Action<ILogger, Exception> _longPolling204 =
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LongPolling204)), "Terminating Long Polling connection by sending 204 response.");

        private static readonly Action<ILogger, Exception> _pollTimedOut =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(PollTimedOut)), "Poll request timed out. Sending 200 response to connection.");

        private static readonly Action<ILogger, long, Exception> _longPollingWritingMessage =
            LoggerMessage.Define<long>(LogLevel.Debug, new EventId(3, nameof(LongPollingWritingMessage)), "Writing a {count} byte message to connection.");

        private static readonly Action<ILogger, Exception> _longPollingDisconnected =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(LongPollingDisconnected)), "Client disconnected from Long Polling endpoint for connection.");

        private static readonly Action<ILogger, Exception> _longPollingTerminated =
            LoggerMessage.Define(LogLevel.Error, new EventId(5, nameof(LongPollingTerminated)), "Long Polling transport was terminated due to an error on connection.");

        // Category: HttpConnectionDispatcher
        private static readonly Action<ILogger, string, Exception> _connectionDisposed =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(ConnectionDisposed)), "Connection Id {connectionId} was disposed.");

        private static readonly Action<ILogger, string, string, Exception> _connectionAlreadyActive =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(2, nameof(ConnectionAlreadyActive)), "Connection Id {connectionId} is already active via {requestId}.");

        private static readonly Action<ILogger, string, string, Exception> _pollCanceled =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(3, nameof(PollCanceled)), "Previous poll canceled for {connectionId} on {requestId}.");

        private static readonly Action<ILogger, Exception> _establishedConnection =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(EstablishedConnection)), "Establishing new connection.");

        private static readonly Action<ILogger, Exception> _resumingConnection =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, nameof(ResumingConnection)), "Resuming existing connection.");

        private static readonly Action<ILogger, long, Exception> _receivedBytes =
            LoggerMessage.Define<long>(LogLevel.Debug, new EventId(6, nameof(ReceivedBytes)), "Received {count} bytes.");

        private static readonly Action<ILogger, TransportType, Exception> _transportNotSupported =
            LoggerMessage.Define<TransportType>(LogLevel.Debug, new EventId(7, nameof(TransportNotSupported)), "{transportType} transport not supported by this endpoint type.");

        private static readonly Action<ILogger, TransportType, TransportType, Exception> _cannotChangeTransport =
            LoggerMessage.Define<TransportType, TransportType>(LogLevel.Debug, new EventId(8, nameof(CannotChangeTransport)), "Cannot change transports mid-connection; currently using {transportType}, requesting {requestedTransport}.");

        private static readonly Action<ILogger, Exception> _postNotallowedForWebsockets =
            LoggerMessage.Define(LogLevel.Debug, new EventId(9, nameof(PostNotAllowedForWebSockets)), "POST requests are not allowed for websocket connections.");

        private static readonly Action<ILogger, Exception> _negotiationRequest =
            LoggerMessage.Define(LogLevel.Debug, new EventId(10, nameof(NegotiationRequest)), "Sending negotiation response.");

        // Category: WebSocketsTransport
        private static readonly Action<ILogger, Exception> _socketOpened =
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(SocketOpened)), "Socket opened.");

        private static readonly Action<ILogger, Exception> _socketClosed =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(SocketClosed)), "Socket closed.");

        private static readonly Action<ILogger, WebSocketCloseStatus?, string, Exception> _clientClosed =
            LoggerMessage.Define<WebSocketCloseStatus?, string>(LogLevel.Debug, new EventId(3, nameof(ClientClosed)), "Client closed connection with status code '{status}' ({description}). Signaling end-of-input to application.");

        private static readonly Action<ILogger, Exception> _waitingForSend =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(WaitingForSend)), "Waiting for the application to finish sending data.");

        private static readonly Action<ILogger, Exception> _failedSending =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, nameof(FailedSending)), "Application failed during sending. Sending InternalServerError close frame.");

        private static readonly Action<ILogger, Exception> _finishedSending =
            LoggerMessage.Define(LogLevel.Debug, new EventId(6, nameof(FinishedSending)), "Application finished sending. Sending close frame.");

        private static readonly Action<ILogger, Exception> _waitingForClose =
            LoggerMessage.Define(LogLevel.Debug, new EventId(7, nameof(WaitingForClose)), "Waiting for the client to close the socket.");

        private static readonly Action<ILogger, Exception> _closeTimedOut =
            LoggerMessage.Define(LogLevel.Debug, new EventId(8, nameof(CloseTimedOut)), "Timed out waiting for client to send the close frame, aborting the connection.");

        private static readonly Action<ILogger, WebSocketMessageType, int, bool, Exception> _messageReceived =
            LoggerMessage.Define<WebSocketMessageType, int, bool>(LogLevel.Debug, new EventId(9, nameof(MessageReceived)), "Message received. Type: {messageType}, size: {size}, EndOfMessage: {endOfMessage}.");

        private static readonly Action<ILogger, int, Exception> _messageToApplication =
            LoggerMessage.Define<int>(LogLevel.Debug, new EventId(10, nameof(MessageToApplication)), "Passing message to application. Payload size: {size}.");

        private static readonly Action<ILogger, long, Exception> _sendPayload =
            LoggerMessage.Define<long>(LogLevel.Debug, new EventId(11, nameof(SendPayload)), "Sending payload: {size} bytes.");

        private static readonly Action<ILogger, Exception> _errorWritingFrame =
            LoggerMessage.Define(LogLevel.Error, new EventId(12, nameof(ErrorWritingFrame)), "Error writing frame.");

        private static readonly Action<ILogger, Exception> _sendFailed =
            LoggerMessage.Define(LogLevel.Trace, new EventId(13, nameof(SendFailed)), "Socket failed to send.");

        // Category: ServerSentEventsTransport
        private static readonly Action<ILogger, long, Exception> _sseWritingMessage =
            LoggerMessage.Define<long>(LogLevel.Debug, new EventId(1, nameof(SSEWritingMessage)), "Writing a {count} byte message.");

        public static void LongPolling204(this ILogger logger)
        {
            _longPolling204(logger, null);
        }

        public static void PollTimedOut(this ILogger logger)
        {
            _pollTimedOut(logger, null);
        }

        public static void LongPollingWritingMessage(this ILogger logger, long count)
        {
            _longPollingWritingMessage(logger, count, null);
        }

        public static void LongPollingDisconnected(this ILogger logger)
        {
            _longPollingDisconnected(logger, null);
        }

        public static void LongPollingTerminated(this ILogger logger, Exception ex)
        {
            _longPollingTerminated(logger, ex);
        }

        public static void ConnectionDisposed(this ILogger logger, string connectionId)
        {
            _connectionDisposed(logger, connectionId, null);
        }

        public static void ConnectionAlreadyActive(this ILogger logger, string connectionId, string requestId)
        {
            _connectionAlreadyActive(logger, connectionId, requestId, null);
        }

        public static void PollCanceled(this ILogger logger, string connectionId, string requestId)
        {
            _pollCanceled(logger, connectionId, requestId, null);
        }

        public static void EstablishedConnection(this ILogger logger)
        {
            _establishedConnection(logger, null);
        }

        public static void ResumingConnection(this ILogger logger)
        {
            _resumingConnection(logger, null);
        }

        public static void ReceivedBytes(this ILogger logger, long count)
        {
            _receivedBytes(logger, count, null);
        }

        public static void TransportNotSupported(this ILogger logger, TransportType transport)
        {
            _transportNotSupported(logger, transport, null);
        }

        public static void CannotChangeTransport(this ILogger logger, TransportType transport, TransportType requestTransport)
        {
            _cannotChangeTransport(logger, transport, requestTransport, null);
        }

        public static void PostNotAllowedForWebSockets(this ILogger logger)
        {
            _postNotallowedForWebsockets(logger, null);
        }

        public static void NegotiationRequest(this ILogger logger)
        {
            _negotiationRequest(logger, null);
        }

        public static void SocketOpened(this ILogger logger)
        {
            _socketOpened(logger, null);
        }

        public static void SocketClosed(this ILogger logger)
        {
            _socketClosed(logger, null);
        }

        public static void ClientClosed(this ILogger logger, WebSocketCloseStatus? closeStatus, string closeDescription)
        {
            _clientClosed(logger, closeStatus, closeDescription, null);
        }

        public static void WaitingForSend(this ILogger logger)
        {
            _waitingForSend(logger, null);
        }

        public static void FailedSending(this ILogger logger)
        {
            _failedSending(logger, null);
        }

        public static void FinishedSending(this ILogger logger)
        {
            _finishedSending(logger, null);
        }

        public static void WaitingForClose(this ILogger logger)
        {
            _waitingForClose(logger, null);
        }

        public static void CloseTimedOut(this ILogger logger)
        {
            _closeTimedOut(logger, null);
        }

        public static void MessageReceived(this ILogger logger, WebSocketMessageType type, int size, bool endOfMessage)
        {
            _messageReceived(logger, type, size, endOfMessage, null);
        }

        public static void MessageToApplication(this ILogger logger, int size)
        {
            _messageToApplication(logger, size, null);
        }

        public static void SendPayload(this ILogger logger, long size)
        {
            _sendPayload(logger, size, null);
        }

        public static void ErrorWritingFrame(this ILogger logger, Exception ex)
        {
            _errorWritingFrame(logger, ex);
        }

        public static void SendFailed(this ILogger logger, Exception ex)
        {
            _sendFailed(logger, ex);
        }

        public static void SSEWritingMessage(this ILogger logger, long count)
        {
            _sseWritingMessage(logger, count, null);
        }
    }
}

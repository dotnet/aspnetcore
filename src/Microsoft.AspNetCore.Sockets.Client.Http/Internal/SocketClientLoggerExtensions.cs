// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client.Internal
{
    internal static class SocketClientLoggerExtensions
    {
        // Category: Shared with LongPollingTransport, WebSocketsTransport and ServerSentEventsTransport
        private static readonly Action<ILogger, DateTime, string, TransferMode, Exception> _startTransport =
            LoggerMessage.Define<DateTime, string, TransferMode>(LogLevel.Information, new EventId(0, nameof(StartTransport)), "{time}: Connection Id {connectionId}: Starting transport. Transfer mode: {transferMode}.");

        private static readonly Action<ILogger, DateTime, string, Exception> _transportStopped =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(1, nameof(TransportStopped)), "{time}: Connection Id {connectionId}: Transport stopped.");

        private static readonly Action<ILogger, DateTime, string, Exception> _startReceive =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(2, nameof(StartReceive)), "{time}: Connection Id {connectionId}: Starting receive loop.");

        private static readonly Action<ILogger, DateTime, string, Exception> _receiveStopped =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(3, nameof(ReceiveStopped)), "{time}: Connection Id {connectionId}: Receive loop stopped.");

        private static readonly Action<ILogger, DateTime, string, Exception> _receiveCanceled =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(4, nameof(ReceiveCanceled)), "{time}: Connection Id {connectionId}: Receive loop canceled.");

        private static readonly Action<ILogger, DateTime, string, Exception> _transportStopping =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(5, nameof(TransportStopping)), "{time}: Connection Id {connectionId}: Transport is stopping.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sendStarted =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(6, nameof(SendStarted)), "{time}: Connection Id {connectionId}: Starting the send loop.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sendStopped =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(7, nameof(SendStopped)), "{time}: Connection Id {connectionId}: Send loop stopped.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sendCanceled =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(8, nameof(SendCanceled)), "{time}: Connection Id {connectionId}: Send loop canceled.");

        // Category: WebSocketsTransport
        private static readonly Action<ILogger, DateTime, string, WebSocketCloseStatus?, Exception> _webSocketClosed =
            LoggerMessage.Define<DateTime, string, WebSocketCloseStatus?>(LogLevel.Information, new EventId(9, nameof(WebSocketClosed)), "{time}: Connection Id {connectionId}: Websocket closed by the server. Close status {closeStatus}.");

        private static readonly Action<ILogger, DateTime, string, WebSocketMessageType, int, bool, Exception> _messageReceived =
            LoggerMessage.Define<DateTime, string, WebSocketMessageType, int, bool>(LogLevel.Debug, new EventId(10, nameof(MessageReceived)), "{time}: Connection Id {connectionId}: Message received. Type: {messageType}, size: {count}, EndOfMessage: {endOfMessage}.");

        private static readonly Action<ILogger, DateTime, string, int, Exception> _messageToApp =
            LoggerMessage.Define<DateTime, string, int>(LogLevel.Debug, new EventId(11, nameof(MessageToApp)), "{time}: Connection Id {connectionId}: Passing message to application. Payload size: {count}.");

        private static readonly Action<ILogger, DateTime, string, int, Exception> _receivedFromApp =
            LoggerMessage.Define<DateTime, string, int>(LogLevel.Debug, new EventId(12, nameof(ReceivedFromApp)), "{time}: Connection Id {connectionId}: Received message from application. Payload size: {count}.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sendMessageCanceled =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(13, nameof(SendMessageCanceled)), "{time}: Connection Id {connectionId}: Sending a message canceled.");

        private static readonly Action<ILogger, DateTime, string, Exception> _errorSendingMessage =
            LoggerMessage.Define<DateTime, string>(LogLevel.Error, new EventId(14, nameof(ErrorSendingMessage)), "{time}: Connection Id {connectionId}: Error while sending a message.");

        private static readonly Action<ILogger, DateTime, string, Exception> _closingWebSocket =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(15, nameof(ClosingWebSocket)), "{time}: Connection Id {connectionId}: Closing WebSocket.");

        private static readonly Action<ILogger, DateTime, string, Exception> _closingWebSocketFailed =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(16, nameof(ClosingWebSocketFailed)), "{time}: Connection Id {connectionId}: Closing webSocket failed.");

        private static readonly Action<ILogger, DateTime, string, Exception> _cancelMessage =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(17, nameof(CancelMessage)), "{time}: Connection Id {connectionId}: Canceled passing message to application.");

        // Category: ServerSentEventsTransport and LongPollingTransport
        private static readonly Action<ILogger, DateTime, string, int, Uri, Exception> _sendingMessages =
            LoggerMessage.Define<DateTime, string, int, Uri>(LogLevel.Debug, new EventId(9, nameof(SendingMessages)), "{time}: Connection Id {connectionId}: Sending {count} message(s) to the server using url: {url}.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sentSuccessfully =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(10, nameof(SentSuccessfully)), "{time}: Connection Id {connectionId}: Message(s) sent successfully.");

        private static readonly Action<ILogger, DateTime, string, Exception> _noMessages =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(11, nameof(NoMessages)), "{time}: Connection Id {connectionId}: No messages in batch to send.");

        private static readonly Action<ILogger, DateTime, string, Uri, Exception> _errorSending =
            LoggerMessage.Define<DateTime, string, Uri>(LogLevel.Error, new EventId(12, nameof(ErrorSending)), "{time}: Connection Id {connectionId}: Error while sending to '{url}'.");

        // Category: ServerSentEventsTransport
        private static readonly Action<ILogger, DateTime, string, Exception> _eventStreamEnded =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(13, nameof(EventStreamEnded)), "{time}: Connection Id {connectionId}: Server-Sent Event Stream ended.");

        // Category: LongPollingTransport
        private static readonly Action<ILogger, DateTime, string, Exception> _closingConnection =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(13, nameof(ClosingConnection)), "{time}: Connection Id {connectionId}: The server is closing the connection.");

        private static readonly Action<ILogger, DateTime, string, Exception> _receivedMessages =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(14, nameof(ReceivedMessages)), "{time}: Connection Id {connectionId}: Received messages from the server.");

        private static readonly Action<ILogger, DateTime, string, Uri, Exception> _errorPolling =
            LoggerMessage.Define<DateTime, string, Uri>(LogLevel.Error, new EventId(15, nameof(ErrorPolling)), "{time}: Connection Id {connectionId}: Error while polling '{pollUrl}'.");

        // Category: HttpConnection
        private static readonly Action<ILogger, DateTime, Exception> _httpConnectionStarting =
            LoggerMessage.Define<DateTime>(LogLevel.Debug, new EventId(0, nameof(HttpConnectionStarting)), "{time}: Starting connection.");

        private static readonly Action<ILogger, DateTime, string, Exception> _httpConnectionClosed =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(1, nameof(HttpConnectionClosed)), "{time}: Connection Id {connectionId}: Connection was closed from a different thread.");

        private static readonly Action<ILogger, DateTime, string, string, Uri, Exception> _startingTransport =
            LoggerMessage.Define<DateTime, string, string, Uri>(LogLevel.Debug, new EventId(2, nameof(StartingTransport)), "{time}: Connection Id {connectionId}: Starting transport '{transport}' with Url: {url}.");

        private static readonly Action<ILogger, DateTime, string, Exception> _raiseConnected =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(3, nameof(RaiseConnected)), "{time}: Connection Id {connectionId}: Raising Connected event.");

        private static readonly Action<ILogger, DateTime, string, Exception> _processRemainingMessages =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(4, nameof(ProcessRemainingMessages)), "{time}: Connection Id {connectionId}: Ensuring all outstanding messages are processed.");

        private static readonly Action<ILogger, DateTime, string, Exception> _drainEvents =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(5, nameof(DrainEvents)), "{time}: Connection Id {connectionId}: Draining event queue.");

        private static readonly Action<ILogger, DateTime, string, Exception> _completeClosed =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(6, nameof(CompleteClosed)), "{time}: Connection Id {connectionId}: Completing Closed task.");

        private static readonly Action<ILogger, DateTime, Uri, Exception> _establishingConnection =
            LoggerMessage.Define<DateTime, Uri>(LogLevel.Debug, new EventId(7, nameof(EstablishingConnection)), "{time}: Establishing Connection at: {url}.");

        private static readonly Action<ILogger, DateTime, Uri, Exception> _errorWithNegotiation =
            LoggerMessage.Define<DateTime, Uri>(LogLevel.Error, new EventId(8, nameof(ErrorWithNegotiation)), "{time}: Failed to start connection. Error getting negotiation response from '{url}'.");

        private static readonly Action<ILogger, DateTime, string, string, Exception> _errorStartingTransport =
            LoggerMessage.Define<DateTime, string, string>(LogLevel.Error, new EventId(9, nameof(ErrorStartingTransport)), "{time}: Connection Id {connectionId}: Failed to start connection. Error starting transport '{transport}'.");

        private static readonly Action<ILogger, DateTime, string, Exception> _httpReceiveStarted =
            LoggerMessage.Define<DateTime, string>(LogLevel.Trace, new EventId(10, nameof(HttpReceiveStarted)), "{time}: Connection Id {connectionId}: Beginning receive loop.");

        private static readonly Action<ILogger, DateTime, string, Exception> _skipRaisingReceiveEvent =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(11, nameof(SkipRaisingReceiveEvent)), "{time}: Connection Id {connectionId}: Message received but connection is not connected. Skipping raising Received event.");

        private static readonly Action<ILogger, DateTime, string, Exception> _scheduleReceiveEvent =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(12, nameof(ScheduleReceiveEvent)), "{time}: Connection Id {connectionId}: Scheduling raising Received event.");

        private static readonly Action<ILogger, DateTime, string, Exception> _raiseReceiveEvent =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(13, nameof(RaiseReceiveEvent)), "{time}: Connection Id {connectionId}: Raising Received event.");

        private static readonly Action<ILogger, DateTime, string, Exception> _failedReadingMessage =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(14, nameof(FailedReadingMessage)), "{time}: Connection Id {connectionId}: Could not read message.");

        private static readonly Action<ILogger, DateTime, string, Exception> _errorReceiving =
            LoggerMessage.Define<DateTime, string>(LogLevel.Error, new EventId(15, nameof(ErrorReceiving)), "{time}: Connection Id {connectionId}: Error receiving message.");

        private static readonly Action<ILogger, DateTime, string, Exception> _endReceive =
            LoggerMessage.Define<DateTime, string>(LogLevel.Trace, new EventId(16, nameof(EndReceive)), "{time}: Connection Id {connectionId}: Ending receive loop.");

        private static readonly Action<ILogger, DateTime, string, Exception> _sendingMessage =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(17, nameof(SendingMessage)), "{time}: Connection Id {connectionId}: Sending message.");

        private static readonly Action<ILogger, DateTime, string, Exception> _stoppingClient =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(18, nameof(StoppingClient)), "{time}: Connection Id {connectionId}: Stopping client.");

        private static readonly Action<ILogger, DateTime, string, string, Exception> _exceptionThrownFromCallback =
            LoggerMessage.Define<DateTime, string, string>(LogLevel.Error, new EventId(19, nameof(ExceptionThrownFromCallback)), "{time}: Connection Id {connectionId}: An exception was thrown from the '{callback}' callback.");

        private static readonly Action<ILogger, DateTime, string, Exception> _disposingClient =
            LoggerMessage.Define<DateTime, string>(LogLevel.Information, new EventId(20, nameof(DisposingClient)), "{time}: Connection Id {connectionId}: Disposing client.");

        private static readonly Action<ILogger, DateTime, string, Exception> _abortingClient =
            LoggerMessage.Define<DateTime, string>(LogLevel.Error, new EventId(21, nameof(AbortingClient)), "{time}: Connection Id {connectionId}: Aborting client.");

        private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
            LoggerMessage.Define(LogLevel.Error, new EventId(22, nameof(ErrorDuringClosedEvent)), "An exception was thrown in the handler for the Closed event.");

        private static readonly Action<ILogger, DateTime, string, Exception> _skippingStop =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(23, nameof(SkippingStop)), "{time}: Connection Id {connectionId}: Skipping stop, connection is already stopped.");

        private static readonly Action<ILogger, DateTime, string, Exception> _skippingDispose =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(24, nameof(SkippingDispose)), "{time}: Connection Id {connectionId}: Skipping dispose, connection is already disposed.");

        private static readonly Action<ILogger, DateTime, string, string, string, Exception> _connectionStateChanged =
            LoggerMessage.Define<DateTime, string, string, string>(LogLevel.Debug, new EventId(25, nameof(ConnectionStateChanged)), "{time}: Connection Id {connectionId}: Connection state changed from {previousState} to {newState}.");

        public static void StartTransport(this ILogger logger, string connectionId, TransferMode transferMode)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _startTransport(logger, DateTime.Now, connectionId, transferMode, null);
            }
        }

        public static void TransportStopped(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _transportStopped(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void StartReceive(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _startReceive(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void TransportStopping(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _transportStopping(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void WebSocketClosed(this ILogger logger, string connectionId, WebSocketCloseStatus? closeStatus)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _webSocketClosed(logger, DateTime.Now, connectionId, closeStatus, null);
            }
        }

        public static void MessageReceived(this ILogger logger, string connectionId, WebSocketMessageType messageType, int count, bool endOfMessage)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _messageReceived(logger, DateTime.Now, connectionId, messageType, count, endOfMessage, null);
            }
        }

        public static void MessageToApp(this ILogger logger, string connectionId, int count)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _messageToApp(logger, DateTime.Now, connectionId, count, null);
            }
        }

        public static void ReceiveCanceled(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _receiveCanceled(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ReceiveStopped(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _receiveStopped(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SendStarted(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sendStarted(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ReceivedFromApp(this ILogger logger, string connectionId, int count)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _receivedFromApp(logger, DateTime.Now, connectionId, count, null);
            }
        }

        public static void SendMessageCanceled(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _sendMessageCanceled(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ErrorSendingMessage(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorSendingMessage(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void SendCanceled(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sendCanceled(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SendStopped(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sendStopped(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ClosingWebSocket(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _closingWebSocket(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ClosingWebSocketFailed(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _closingWebSocketFailed(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void CancelMessage(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _cancelMessage(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SendingMessages(this ILogger logger, string connectionId, int count, Uri url)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sendingMessages(logger, DateTime.Now, connectionId, count, url, null);
            }
        }

        public static void SentSuccessfully(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sentSuccessfully(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void NoMessages(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _noMessages(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ErrorSending(this ILogger logger, string connectionId, Uri url, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorSending(logger, DateTime.Now, connectionId, url, exception);
            }
        }

        public static void EventStreamEnded(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _eventStreamEnded(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ClosingConnection(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _closingConnection(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ReceivedMessages(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _receivedMessages(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ErrorPolling(this ILogger logger, string connectionId, Uri pollUrl, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorPolling(logger, DateTime.Now, connectionId, pollUrl, exception);
            }
        }

        public static void HttpConnectionStarting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _httpConnectionStarting(logger, DateTime.Now, null);
            }
        }

        public static void HttpConnectionClosed(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _httpConnectionClosed(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void StartingTransport(this ILogger logger, string connectionId, string transport, Uri url)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _startingTransport(logger, DateTime.Now, connectionId, transport, url, null);
            }
        }

        public static void RaiseConnected(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _raiseConnected(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ProcessRemainingMessages(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _processRemainingMessages(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void DrainEvents(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _drainEvents(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void CompleteClosed(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _completeClosed(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void EstablishingConnection(this ILogger logger, Uri url)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _establishingConnection(logger, DateTime.Now, url, null);
            }
        }

        public static void ErrorWithNegotiation(this ILogger logger, Uri url, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorWithNegotiation(logger, DateTime.Now, url, exception);
            }
        }

        public static void ErrorStartingTransport(this ILogger logger, string connectionId, string transport, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorStartingTransport(logger, DateTime.Now, connectionId, transport, exception);
            }
        }

        public static void HttpReceiveStarted(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _httpReceiveStarted(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SkipRaisingReceiveEvent(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _skipRaisingReceiveEvent(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ScheduleReceiveEvent(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _scheduleReceiveEvent(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void RaiseReceiveEvent(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _raiseReceiveEvent(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void FailedReadingMessage(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _failedReadingMessage(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ErrorReceiving(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorReceiving(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void EndReceive(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _endReceive(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SendingMessage(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _sendingMessage(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void AbortingClient(this ILogger logger, string connectionId, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _abortingClient(logger, DateTime.Now, connectionId, ex);
            }
        }

        public static void StoppingClient(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _stoppingClient(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void DisposingClient(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _disposingClient(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void SkippingDispose(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _skippingDispose(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ConnectionStateChanged(this ILogger logger, string connectionId, HttpConnection.ConnectionState previousState, HttpConnection.ConnectionState newState)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _connectionStateChanged(logger, DateTime.Now, connectionId, previousState.ToString(), newState.ToString(), null);
            }
        }

        public static void SkippingStop(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _skippingStop(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ExceptionThrownFromCallback(this ILogger logger, string connectionId, string callbackName, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _exceptionThrownFromCallback(logger, DateTime.Now, connectionId, callbackName, exception);
            }
        }

        public static void ErrorDuringClosedEvent(this ILogger logger, Exception exception)
        {
            _errorDuringClosedEvent(logger, exception);
        }
    }
}

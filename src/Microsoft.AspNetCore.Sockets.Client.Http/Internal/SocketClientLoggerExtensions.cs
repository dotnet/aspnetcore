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
        private static readonly Action<ILogger, TransferMode, Exception> _startTransport =
            LoggerMessage.Define<TransferMode>(LogLevel.Information, new EventId(1, nameof(StartTransport)), "Starting transport. Transfer mode: {transferMode}.");

        private static readonly Action<ILogger, Exception> _transportStopped =
            LoggerMessage.Define(LogLevel.Debug, new EventId(2, nameof(TransportStopped)), "Transport stopped.");

        private static readonly Action<ILogger, Exception> _startReceive =
            LoggerMessage.Define(LogLevel.Debug, new EventId(3, nameof(StartReceive)), "Starting receive loop.");

        private static readonly Action<ILogger, Exception> _receiveStopped =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(ReceiveStopped)), "Receive loop stopped.");

        private static readonly Action<ILogger, Exception> _receiveCanceled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, nameof(ReceiveCanceled)), "Receive loop canceled.");

        private static readonly Action<ILogger, Exception> _transportStopping =
            LoggerMessage.Define(LogLevel.Information, new EventId(6, nameof(TransportStopping)), "Transport is stopping.");

        private static readonly Action<ILogger, Exception> _sendStarted =
            LoggerMessage.Define(LogLevel.Debug, new EventId(7, nameof(SendStarted)), "Starting the send loop.");

        private static readonly Action<ILogger, Exception> _sendStopped =
            LoggerMessage.Define(LogLevel.Debug, new EventId(8, nameof(SendStopped)), "Send loop stopped.");

        private static readonly Action<ILogger, Exception> _sendCanceled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(9, nameof(SendCanceled)), "Send loop canceled.");

        // Category: WebSocketsTransport
        private static readonly Action<ILogger, WebSocketCloseStatus?, Exception> _webSocketClosed =
            LoggerMessage.Define<WebSocketCloseStatus?>(LogLevel.Information, new EventId(10, nameof(WebSocketClosed)), "Websocket closed by the server. Close status {closeStatus}.");

        private static readonly Action<ILogger, WebSocketMessageType, int, bool, Exception> _messageReceived =
            LoggerMessage.Define<WebSocketMessageType, int, bool>(LogLevel.Debug, new EventId(11, nameof(MessageReceived)), "Message received. Type: {messageType}, size: {count}, EndOfMessage: {endOfMessage}.");

        private static readonly Action<ILogger, int, Exception> _messageToApp =
            LoggerMessage.Define<int>(LogLevel.Debug, new EventId(12, nameof(MessageToApp)), "Passing message to application. Payload size: {count}.");

        private static readonly Action<ILogger, long, Exception> _receivedFromApp =
            LoggerMessage.Define<long>(LogLevel.Debug, new EventId(13, nameof(ReceivedFromApp)), "Received message from application. Payload size: {count}.");

        private static readonly Action<ILogger, Exception> _sendMessageCanceled =
            LoggerMessage.Define(LogLevel.Information, new EventId(14, nameof(SendMessageCanceled)), "Sending a message canceled.");

        private static readonly Action<ILogger, Exception> _errorSendingMessage =
            LoggerMessage.Define(LogLevel.Error, new EventId(15, nameof(ErrorSendingMessage)), "Error while sending a message.");

        private static readonly Action<ILogger, Exception> _closingWebSocket =
            LoggerMessage.Define(LogLevel.Information, new EventId(16, nameof(ClosingWebSocket)), "Closing WebSocket.");

        private static readonly Action<ILogger, Exception> _closingWebSocketFailed =
            LoggerMessage.Define(LogLevel.Information, new EventId(17, nameof(ClosingWebSocketFailed)), "Closing webSocket failed.");

        private static readonly Action<ILogger, Exception> _cancelMessage =
            LoggerMessage.Define(LogLevel.Debug, new EventId(18, nameof(CancelMessage)), "Canceled passing message to application.");

        // Category: ServerSentEventsTransport and LongPollingTransport
        private static readonly Action<ILogger, long, Uri, Exception> _sendingMessages =
            LoggerMessage.Define<long, Uri>(LogLevel.Debug, new EventId(10, nameof(SendingMessages)), "Sending {count} bytes to the server using url: {url}.");

        private static readonly Action<ILogger, Exception> _sentSuccessfully =
            LoggerMessage.Define(LogLevel.Debug, new EventId(11, nameof(SentSuccessfully)), "Message(s) sent successfully.");

        private static readonly Action<ILogger, Exception> _noMessages =
            LoggerMessage.Define(LogLevel.Debug, new EventId(12, nameof(NoMessages)), "No messages in batch to send.");

        private static readonly Action<ILogger, Uri, Exception> _errorSending =
            LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(13, nameof(ErrorSending)), "Error while sending to '{url}'.");

        // Category: ServerSentEventsTransport
        private static readonly Action<ILogger, Exception> _eventStreamEnded =
            LoggerMessage.Define(LogLevel.Debug, new EventId(14, nameof(EventStreamEnded)), "Server-Sent Event Stream ended.");

        // Category: LongPollingTransport
        private static readonly Action<ILogger, Exception> _closingConnection =
            LoggerMessage.Define(LogLevel.Debug, new EventId(14, nameof(ClosingConnection)), "The server is closing the connection.");

        private static readonly Action<ILogger, Exception> _receivedMessages =
            LoggerMessage.Define(LogLevel.Debug, new EventId(15, nameof(ReceivedMessages)), "Received messages from the server.");

        private static readonly Action<ILogger, Uri, Exception> _errorPolling =
            LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(16, nameof(ErrorPolling)), "Error while polling '{pollUrl}'.");

        // Category: HttpConnection
        private static readonly Action<ILogger, Exception> _httpConnectionStarting =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(HttpConnectionStarting)), "Starting connection.");

        private static readonly Action<ILogger, Exception> _httpConnectionClosed =
            LoggerMessage.Define(LogLevel.Debug, new EventId(2, nameof(HttpConnectionClosed)), "Connection was closed from a different thread.");

        private static readonly Action<ILogger, string, Uri, Exception> _startingTransport =
            LoggerMessage.Define<string, Uri>(LogLevel.Debug, new EventId(3, nameof(StartingTransport)), "Starting transport '{transport}' with Url: {url}.");

        private static readonly Action<ILogger, Exception> _raiseConnected =
            LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(RaiseConnected)), "Raising Connected event.");

        private static readonly Action<ILogger, Exception> _processRemainingMessages =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, nameof(ProcessRemainingMessages)), "Ensuring all outstanding messages are processed.");

        private static readonly Action<ILogger, Exception> _drainEvents =
            LoggerMessage.Define(LogLevel.Debug, new EventId(6, nameof(DrainEvents)), "Draining event queue.");

        private static readonly Action<ILogger, Exception> _completeClosed =
            LoggerMessage.Define(LogLevel.Debug, new EventId(7, nameof(CompleteClosed)), "Completing Closed task.");

        private static readonly Action<ILogger, Uri, Exception> _establishingConnection =
            LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(8, nameof(EstablishingConnection)), "Establishing Connection at: {url}.");

        private static readonly Action<ILogger, Uri, Exception> _errorWithNegotiation =
            LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(9, nameof(ErrorWithNegotiation)), "Failed to start connection. Error getting negotiation response from '{url}'.");

        private static readonly Action<ILogger, string, Exception> _errorStartingTransport =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(10, nameof(ErrorStartingTransport)), "Failed to start connection. Error starting transport '{transport}'.");

        private static readonly Action<ILogger, Exception> _httpReceiveStarted =
            LoggerMessage.Define(LogLevel.Trace, new EventId(11, nameof(HttpReceiveStarted)), "Beginning receive loop.");

        private static readonly Action<ILogger, Exception> _skipRaisingReceiveEvent =
            LoggerMessage.Define(LogLevel.Debug, new EventId(12, nameof(SkipRaisingReceiveEvent)), "Message received but connection is not connected. Skipping raising Received event.");

        private static readonly Action<ILogger, Exception> _scheduleReceiveEvent =
            LoggerMessage.Define(LogLevel.Debug, new EventId(13, nameof(ScheduleReceiveEvent)), "Scheduling raising Received event.");

        private static readonly Action<ILogger, Exception> _raiseReceiveEvent =
            LoggerMessage.Define(LogLevel.Debug, new EventId(14, nameof(RaiseReceiveEvent)), "Raising Received event.");

        private static readonly Action<ILogger, Exception> _failedReadingMessage =
            LoggerMessage.Define(LogLevel.Debug, new EventId(15, nameof(FailedReadingMessage)), "Could not read message.");

        private static readonly Action<ILogger, Exception> _errorReceiving =
            LoggerMessage.Define(LogLevel.Error, new EventId(16, nameof(ErrorReceiving)), "Error receiving message.");

        private static readonly Action<ILogger, Exception> _endReceive =
            LoggerMessage.Define(LogLevel.Trace, new EventId(17, nameof(EndReceive)), "Ending receive loop.");

        private static readonly Action<ILogger, Exception> _sendingMessage =
            LoggerMessage.Define(LogLevel.Debug, new EventId(18, nameof(SendingMessage)), "Sending message.");

        private static readonly Action<ILogger, Exception> _stoppingClient =
            LoggerMessage.Define(LogLevel.Information, new EventId(19, nameof(StoppingClient)), "Stopping client.");

        private static readonly Action<ILogger, string, Exception> _exceptionThrownFromCallback =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(20, nameof(ExceptionThrownFromCallback)), "An exception was thrown from the '{callback}' callback.");

        private static readonly Action<ILogger, Exception> _disposingClient =
            LoggerMessage.Define(LogLevel.Information, new EventId(21, nameof(DisposingClient)), "Disposing client.");

        private static readonly Action<ILogger, Exception> _abortingClient =
            LoggerMessage.Define(LogLevel.Error, new EventId(22, nameof(AbortingClient)), "Aborting client.");

        private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
            LoggerMessage.Define(LogLevel.Error, new EventId(23, nameof(ErrorDuringClosedEvent)), "An exception was thrown in the handler for the Closed event.");

        private static readonly Action<ILogger, Exception> _skippingStop =
            LoggerMessage.Define(LogLevel.Debug, new EventId(24, nameof(SkippingStop)), "Skipping stop, connection is already stopped.");

        private static readonly Action<ILogger, Exception> _skippingDispose =
            LoggerMessage.Define(LogLevel.Debug, new EventId(25, nameof(SkippingDispose)), "Skipping dispose, connection is already disposed.");

        private static readonly Action<ILogger, string, string, Exception> _connectionStateChanged =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(26, nameof(ConnectionStateChanged)), "Connection state changed from {previousState} to {newState}.");

        public static void StartTransport(this ILogger logger, TransferMode transferMode)
        {
            _startTransport(logger, transferMode, null);
        }

        public static void TransportStopped(this ILogger logger, Exception exception)
        {
            _transportStopped(logger, exception);
        }

        public static void StartReceive(this ILogger logger)
        {
            _startReceive(logger, null);
        }

        public static void TransportStopping(this ILogger logger)
        {
            _transportStopping(logger, null);
        }

        public static void WebSocketClosed(this ILogger logger, WebSocketCloseStatus? closeStatus)
        {
            _webSocketClosed(logger, closeStatus, null);
        }

        public static void MessageReceived(this ILogger logger, WebSocketMessageType messageType, int count, bool endOfMessage)
        {
            _messageReceived(logger, messageType, count, endOfMessage, null);
        }

        public static void MessageToApp(this ILogger logger, int count)
        {
            _messageToApp(logger, count, null);
        }

        public static void ReceiveCanceled(this ILogger logger)
        {
            _receiveCanceled(logger, null);
        }

        public static void ReceiveStopped(this ILogger logger)
        {
            _receiveStopped(logger, null);
        }

        public static void SendStarted(this ILogger logger)
        {
            _sendStarted(logger, null);
        }

        public static void ReceivedFromApp(this ILogger logger, long count)
        {
            _receivedFromApp(logger, count, null);
        }

        public static void SendMessageCanceled(this ILogger logger)
        {
            _sendMessageCanceled(logger, null);
        }

        public static void ErrorSendingMessage(this ILogger logger, Exception exception)
        {
            _errorSendingMessage(logger, exception);
        }

        public static void SendCanceled(this ILogger logger)
        {
            _sendCanceled(logger, null);
        }

        public static void SendStopped(this ILogger logger)
        {
            _sendStopped(logger, null);
        }

        public static void ClosingWebSocket(this ILogger logger)
        {
            _closingWebSocket(logger, null);
        }

        public static void ClosingWebSocketFailed(this ILogger logger, Exception exception)
        {
            _closingWebSocketFailed(logger, exception);
        }

        public static void CancelMessage(this ILogger logger)
        {
            _cancelMessage(logger, null);
        }

        public static void SendingMessages(this ILogger logger, long count, Uri url)
        {
            _sendingMessages(logger, count, url, null);
        }

        public static void SentSuccessfully(this ILogger logger)
        {
            _sentSuccessfully(logger, null);
        }

        public static void NoMessages(this ILogger logger)
        {
            _noMessages(logger, null);
        }

        public static void ErrorSending(this ILogger logger, Uri url, Exception exception)
        {
            _errorSending(logger, url, exception);
        }

        public static void EventStreamEnded(this ILogger logger)
        {
            _eventStreamEnded(logger, null);
        }

        public static void ClosingConnection(this ILogger logger)
        {
            _closingConnection(logger, null);
        }

        public static void ReceivedMessages(this ILogger logger)
        {
            _receivedMessages(logger, null);
        }

        public static void ErrorPolling(this ILogger logger, Uri pollUrl, Exception exception)
        {
            _errorPolling(logger, pollUrl, exception);
        }

        public static void HttpConnectionStarting(this ILogger logger)
        {
            _httpConnectionStarting(logger, null);
        }

        public static void HttpConnectionClosed(this ILogger logger)
        {
            _httpConnectionClosed(logger, null);
        }

        public static void StartingTransport(this ILogger logger, ITransport transport, Uri url)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _startingTransport(logger, transport.GetType().Name, url, null);
            }
        }

        public static void RaiseConnected(this ILogger logger)
        {
            _raiseConnected(logger, null);
        }

        public static void ProcessRemainingMessages(this ILogger logger)
        {
            _processRemainingMessages(logger, null);
        }

        public static void DrainEvents(this ILogger logger)
        {
            _drainEvents(logger, null);
        }

        public static void CompleteClosed(this ILogger logger)
        {
            _completeClosed(logger, null);
        }

        public static void EstablishingConnection(this ILogger logger, Uri url)
        {
            _establishingConnection(logger, url, null);
        }

        public static void ErrorWithNegotiation(this ILogger logger, Uri url, Exception exception)
        {
            _errorWithNegotiation(logger, url, exception);
        }

        public static void ErrorStartingTransport(this ILogger logger, ITransport transport, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _errorStartingTransport(logger, transport.GetType().Name, exception);
            }
        }

        public static void HttpReceiveStarted(this ILogger logger)
        {
            _httpReceiveStarted(logger, null);
        }

        public static void SkipRaisingReceiveEvent(this ILogger logger)
        {
            _skipRaisingReceiveEvent(logger, null);
        }

        public static void ScheduleReceiveEvent(this ILogger logger)
        {
            _scheduleReceiveEvent(logger, null);
        }

        public static void RaiseReceiveEvent(this ILogger logger)
        {
            _raiseReceiveEvent(logger, null);
        }

        public static void FailedReadingMessage(this ILogger logger)
        {
            _failedReadingMessage(logger, null);
        }

        public static void ErrorReceiving(this ILogger logger, Exception exception)
        {
            _errorReceiving(logger, exception);
        }

        public static void EndReceive(this ILogger logger)
        {
            _endReceive(logger, null);
        }

        public static void SendingMessage(this ILogger logger)
        {
            _sendingMessage(logger, null);
        }

        public static void AbortingClient(this ILogger logger, Exception ex)
        {
            _abortingClient(logger, ex);
        }

        public static void StoppingClient(this ILogger logger)
        {
            _stoppingClient(logger, null);
        }

        public static void DisposingClient(this ILogger logger)
        {
            _disposingClient(logger, null);
        }

        public static void SkippingDispose(this ILogger logger)
        {
            _skippingDispose(logger, null);
        }

        public static void ConnectionStateChanged(this ILogger logger, HttpConnection.ConnectionState previousState, HttpConnection.ConnectionState newState)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _connectionStateChanged(logger, previousState.ToString(), newState.ToString(), null);
            }
        }

        public static void SkippingStop(this ILogger logger)
        {
            _skippingStop(logger, null);
        }

        public static void ExceptionThrownFromCallback(this ILogger logger, string callbackName, Exception exception)
        {
            _exceptionThrownFromCallback(logger, callbackName, exception);
        }

        public static void ErrorDuringClosedEvent(this ILogger logger, Exception exception)
        {
            _errorDuringClosedEvent(logger, exception);
        }
    }
}

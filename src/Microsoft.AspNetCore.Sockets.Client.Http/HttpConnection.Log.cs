// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public partial class HttpConnection
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception> _httpConnectionStarting =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1, "HttpConnectionStarting"), "Starting connection.");

            private static readonly Action<ILogger, Exception> _httpConnectionClosed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "HttpConnectionClosed"), "Connection was closed from a different thread.");

            private static readonly Action<ILogger, string, Uri, Exception> _startingTransport =
                LoggerMessage.Define<string, Uri>(LogLevel.Debug, new EventId(3, "StartingTransport"), "Starting transport '{transport}' with Url: {url}.");

            private static readonly Action<ILogger, Exception> _processRemainingMessages =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ProcessRemainingMessages"), "Ensuring all outstanding messages are processed.");

            private static readonly Action<ILogger, Exception> _drainEvents =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "DrainEvents"), "Draining event queue.");

            private static readonly Action<ILogger, Exception> _completeClosed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "CompleteClosed"), "Completing Closed task.");

            private static readonly Action<ILogger, Uri, Exception> _establishingConnection =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(7, "EstablishingConnection"), "Establishing Connection at: {url}.");

            private static readonly Action<ILogger, Uri, Exception> _errorWithNegotiation =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(8, "ErrorWithNegotiation"), "Failed to start connection. Error getting negotiation response from '{url}'.");

            private static readonly Action<ILogger, string, Exception> _errorStartingTransport =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(9, "ErrorStartingTransport"), "Failed to start connection. Error starting transport '{transport}'.");

            private static readonly Action<ILogger, Exception> _httpReceiveStarted =
                LoggerMessage.Define(LogLevel.Trace, new EventId(10, "HttpReceiveStarted"), "Beginning receive loop.");

            private static readonly Action<ILogger, Exception> _skipRaisingReceiveEvent =
                LoggerMessage.Define(LogLevel.Debug, new EventId(11, "SkipRaisingReceiveEvent"), "Message received but connection is not connected. Skipping raising Received event.");

            private static readonly Action<ILogger, Exception> _scheduleReceiveEvent =
                LoggerMessage.Define(LogLevel.Debug, new EventId(12, "ScheduleReceiveEvent"), "Scheduling raising Received event.");

            private static readonly Action<ILogger, Exception> _raiseReceiveEvent =
                LoggerMessage.Define(LogLevel.Debug, new EventId(13, "RaiseReceiveEvent"), "Raising Received event.");

            private static readonly Action<ILogger, Exception> _failedReadingMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(14, "FailedReadingMessage"), "Could not read message.");

            private static readonly Action<ILogger, Exception> _errorReceiving =
                LoggerMessage.Define(LogLevel.Error, new EventId(15, "ErrorReceiving"), "Error receiving message.");

            private static readonly Action<ILogger, Exception> _endReceive =
                LoggerMessage.Define(LogLevel.Trace, new EventId(16, "EndReceive"), "Ending receive loop.");

            private static readonly Action<ILogger, Exception> _sendingMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(17, "SendingMessage"), "Sending message.");

            private static readonly Action<ILogger, Exception> _stoppingClient =
                LoggerMessage.Define(LogLevel.Information, new EventId(18, "StoppingClient"), "Stopping client.");

            private static readonly Action<ILogger, string, Exception> _exceptionThrownFromCallback =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(19, "ExceptionThrownFromCallback"), "An exception was thrown from the '{callback}' callback.");

            private static readonly Action<ILogger, Exception> _disposingClient =
                LoggerMessage.Define(LogLevel.Information, new EventId(20, "DisposingClient"), "Disposing client.");

            private static readonly Action<ILogger, Exception> _abortingClient =
                LoggerMessage.Define(LogLevel.Error, new EventId(21, "AbortingClient"), "Aborting client.");

            private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
                LoggerMessage.Define(LogLevel.Error, new EventId(22, "ErrorDuringClosedEvent"), "An exception was thrown in the handler for the Closed event.");

            private static readonly Action<ILogger, Exception> _skippingStop =
                LoggerMessage.Define(LogLevel.Debug, new EventId(23, "SkippingStop"), "Skipping stop, connection is already stopped.");

            private static readonly Action<ILogger, Exception> _skippingDispose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(24, "SkippingDispose"), "Skipping dispose, connection is already disposed.");

            private static readonly Action<ILogger, string, string, Exception> _connectionStateChanged =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(25, "ConnectionStateChanged"), "Connection state changed from {previousState} to {newState}.");

            public static void HttpConnectionStarting(ILogger logger)
            {
                _httpConnectionStarting(logger, null);
            }

            public static void HttpConnectionClosed(ILogger logger)
            {
                _httpConnectionClosed(logger, null);
            }

            public static void StartingTransport(ILogger logger, ITransport transport, Uri url)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _startingTransport(logger, transport.GetType().Name, url, null);
                }
            }

            public static void ProcessRemainingMessages(ILogger logger)
            {
                _processRemainingMessages(logger, null);
            }

            public static void DrainEvents(ILogger logger)
            {
                _drainEvents(logger, null);
            }

            public static void CompleteClosed(ILogger logger)
            {
                _completeClosed(logger, null);
            }

            public static void EstablishingConnection(ILogger logger, Uri url)
            {
                _establishingConnection(logger, url, null);
            }

            public static void ErrorWithNegotiation(ILogger logger, Uri url, Exception exception)
            {
                _errorWithNegotiation(logger, url, exception);
            }

            public static void ErrorStartingTransport(ILogger logger, ITransport transport, Exception exception)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _errorStartingTransport(logger, transport.GetType().Name, exception);
                }
            }

            public static void HttpReceiveStarted(ILogger logger)
            {
                _httpReceiveStarted(logger, null);
            }

            public static void SkipRaisingReceiveEvent(ILogger logger)
            {
                _skipRaisingReceiveEvent(logger, null);
            }

            public static void ScheduleReceiveEvent(ILogger logger)
            {
                _scheduleReceiveEvent(logger, null);
            }

            public static void RaiseReceiveEvent(ILogger logger)
            {
                _raiseReceiveEvent(logger, null);
            }

            public static void FailedReadingMessage(ILogger logger)
            {
                _failedReadingMessage(logger, null);
            }

            public static void ErrorReceiving(ILogger logger, Exception exception)
            {
                _errorReceiving(logger, exception);
            }

            public static void EndReceive(ILogger logger)
            {
                _endReceive(logger, null);
            }

            public static void SendingMessage(ILogger logger)
            {
                _sendingMessage(logger, null);
            }

            public static void AbortingClient(ILogger logger, Exception ex)
            {
                _abortingClient(logger, ex);
            }

            public static void StoppingClient(ILogger logger)
            {
                _stoppingClient(logger, null);
            }

            public static void DisposingClient(ILogger logger)
            {
                _disposingClient(logger, null);
            }

            public static void SkippingDispose(ILogger logger)
            {
                _skippingDispose(logger, null);
            }

            public static void ConnectionStateChanged(ILogger logger, HttpConnection.ConnectionState previousState, HttpConnection.ConnectionState newState)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _connectionStateChanged(logger, previousState.ToString(), newState.ToString(), null);
                }
            }

            public static void SkippingStop(ILogger logger)
            {
                _skippingStop(logger, null);
            }

            public static void ExceptionThrownFromCallback(ILogger logger, string callbackName, Exception exception)
            {
                _exceptionThrownFromCallback(logger, callbackName, exception);
            }

            public static void ErrorDuringClosedEvent(ILogger logger, Exception exception)
            {
                _errorDuringClosedEvent(logger, exception);
            }
        }
    }
}

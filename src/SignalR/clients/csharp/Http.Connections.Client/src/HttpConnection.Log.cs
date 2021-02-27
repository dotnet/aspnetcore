// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    public partial class HttpConnection
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _starting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(1, "Starting"), "Starting HttpConnection.");

            private static readonly Action<ILogger, Exception?> _skippingStart =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "SkippingStart"), "Skipping start, connection is already started.");

            private static readonly Action<ILogger, Exception?> _started =
                LoggerMessage.Define(LogLevel.Information, new EventId(3, "Started"), "HttpConnection Started.");

            private static readonly Action<ILogger, Exception?> _disposingHttpConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "DisposingHttpConnection"), "Disposing HttpConnection.");

            private static readonly Action<ILogger, Exception?> _skippingDispose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "SkippingDispose"), "Skipping dispose, connection is already disposed.");

            private static readonly Action<ILogger, Exception?> _disposed =
                LoggerMessage.Define(LogLevel.Information, new EventId(6, "Disposed"), "HttpConnection Disposed.");

            private static readonly Action<ILogger, string, Uri, Exception?> _startingTransport =
                LoggerMessage.Define<string, Uri>(LogLevel.Debug, new EventId(7, "StartingTransport"), "Starting transport '{Transport}' with Url: {Url}.");

            private static readonly Action<ILogger, Uri, Exception?> _establishingConnection =
                LoggerMessage.Define<Uri>(LogLevel.Debug, new EventId(8, "EstablishingConnection"), "Establishing connection with server at '{Url}'.");

            private static readonly Action<ILogger, string, Exception?> _connectionEstablished =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(9, "Established"), "Established connection '{ConnectionId}' with the server.");

            private static readonly Action<ILogger, Uri, Exception> _errorWithNegotiation =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(10, "ErrorWithNegotiation"), "Failed to start connection. Error getting negotiation response from '{Url}'.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _errorStartingTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Error, new EventId(11, "ErrorStartingTransport"), "Failed to start connection. Error starting transport '{Transport}'.");

            private static readonly Action<ILogger, string, Exception?> _transportNotSupported =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(12, "TransportNotSupported"), "Skipping transport {TransportName} because it is not supported by this client.");

            private static readonly Action<ILogger, string, string, Exception?> _transportDoesNotSupportTransferFormat =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(13, "TransportDoesNotSupportTransferFormat"), "Skipping transport {TransportName} because it does not support the requested transfer format '{TransferFormat}'.");

            private static readonly Action<ILogger, string, Exception?> _transportDisabledByClient =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(14, "TransportDisabledByClient"), "Skipping transport {TransportName} because it was disabled by the client.");

            private static readonly Action<ILogger, string, Exception> _transportFailed =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(15, "TransportFailed"), "Skipping transport {TransportName} because it failed to initialize.");

            private static readonly Action<ILogger, Exception?> _webSocketsNotSupportedByOperatingSystem =
                LoggerMessage.Define(LogLevel.Debug, new EventId(16, "WebSocketsNotSupportedByOperatingSystem"), "Skipping WebSockets because they are not supported by the operating system.");

            private static readonly Action<ILogger, Exception> _transportThrewExceptionOnStop =
                LoggerMessage.Define(LogLevel.Error, new EventId(17, "TransportThrewExceptionOnStop"), "The transport threw an exception while stopping.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _transportStarted =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Debug, new EventId(18, "TransportStarted"), "Transport '{Transport}' started.");

            private static readonly Action<ILogger, Exception?> _serverSentEventsNotSupportedByBrowser =
                LoggerMessage.Define(LogLevel.Debug, new EventId(19, "ServerSentEventsNotSupportedByBrowser"), "Skipping ServerSentEvents because they are not supported by the browser.");

            private static readonly Action<ILogger, Exception?> _cookiesNotSupported =
                LoggerMessage.Define(LogLevel.Trace, new EventId(20, "CookiesNotSupported"), "Cookies are not supported on this platform.");

            public static void Starting(ILogger logger)
            {
                _starting(logger, null);
            }

            public static void SkippingStart(ILogger logger)
            {
                _skippingStart(logger, null);
            }

            public static void Started(ILogger logger)
            {
                _started(logger, null);
            }

            public static void DisposingHttpConnection(ILogger logger)
            {
                _disposingHttpConnection(logger, null);
            }

            public static void SkippingDispose(ILogger logger)
            {
                _skippingDispose(logger, null);
            }

            public static void Disposed(ILogger logger)
            {
                _disposed(logger, null);
            }

            public static void StartingTransport(ILogger logger, HttpTransportType transportType, Uri url)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _startingTransport(logger, transportType.ToString(), url, null);
                }
            }

            public static void EstablishingConnection(ILogger logger, Uri url)
            {
                _establishingConnection(logger, url, null);
            }

            public static void ConnectionEstablished(ILogger logger, string connectionId)
            {
                _connectionEstablished(logger, connectionId, null);
            }

            public static void ErrorWithNegotiation(ILogger logger, Uri url, Exception exception)
            {
                _errorWithNegotiation(logger, url, exception);
            }

            public static void ErrorStartingTransport(ILogger logger, HttpTransportType transportType, Exception exception)
            {
                _errorStartingTransport(logger, transportType, exception);
            }

            public static void TransportNotSupported(ILogger logger, string transport)
            {
                _transportNotSupported(logger, transport, null);
            }

            public static void TransportDoesNotSupportTransferFormat(ILogger logger, HttpTransportType transport, TransferFormat transferFormat)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _transportDoesNotSupportTransferFormat(logger, transport.ToString(), transferFormat.ToString(), null);
                }
            }

            public static void TransportDisabledByClient(ILogger logger, HttpTransportType transport)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _transportDisabledByClient(logger, transport.ToString(), null);
                }
            }

            public static void TransportFailed(ILogger logger, HttpTransportType transport, Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _transportFailed(logger, transport.ToString(), ex);
                }
            }

            public static void WebSocketsNotSupportedByOperatingSystem(ILogger logger)
            {
                _webSocketsNotSupportedByOperatingSystem(logger, null);
            }

            public static void TransportThrewExceptionOnStop(ILogger logger, Exception ex)
            {
                _transportThrewExceptionOnStop(logger, ex);
            }

            public static void TransportStarted(ILogger logger, HttpTransportType transportType)
            {
                _transportStarted(logger, transportType, null);
            }

            public static void ServerSentEventsNotSupportedByBrowser(ILogger logger)
            {
                _serverSentEventsNotSupportedByBrowser(logger, null);
            }

            public static void CookiesNotSupported(ILogger logger)
            {
                _cookiesNotSupported(logger, null);
            }
        }
    }
}

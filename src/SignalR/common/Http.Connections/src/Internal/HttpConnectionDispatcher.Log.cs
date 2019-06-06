// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public partial class HttpConnectionDispatcher
    {
        internal static class Log
        {
            private static readonly Action<ILogger, string, Exception> _connectionDisposed =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "ConnectionDisposed"), "Connection {TransportConnectionId} was disposed.");

            private static readonly Action<ILogger, string, string, Exception> _connectionAlreadyActive =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(2, "ConnectionAlreadyActive"), "Connection {TransportConnectionId} is already active via {RequestId}.");

            private static readonly Action<ILogger, string, string, Exception> _pollCanceled =
                LoggerMessage.Define<string, string>(LogLevel.Trace, new EventId(3, "PollCanceled"), "Previous poll canceled for {TransportConnectionId} on {RequestId}.");

            private static readonly Action<ILogger, Exception> _establishedConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "EstablishedConnection"), "Establishing new connection.");

            private static readonly Action<ILogger, Exception> _resumingConnection =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ResumingConnection"), "Resuming existing connection.");

            private static readonly Action<ILogger, long, Exception> _receivedBytes =
                LoggerMessage.Define<long>(LogLevel.Trace, new EventId(6, "ReceivedBytes"), "Received {Count} bytes.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportNotSupported =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Debug, new EventId(7, "TransportNotSupported"), "{TransportType} transport not supported by this connection handler.");

            private static readonly Action<ILogger, HttpTransportType, HttpTransportType, Exception> _cannotChangeTransport =
                LoggerMessage.Define<HttpTransportType, HttpTransportType>(LogLevel.Error, new EventId(8, "CannotChangeTransport"), "Cannot change transports mid-connection; currently using {TransportType}, requesting {RequestedTransport}.");

            private static readonly Action<ILogger, Exception> _postNotallowedForWebsockets =
                LoggerMessage.Define(LogLevel.Debug, new EventId(9, "PostNotAllowedForWebSockets"), "POST requests are not allowed for websocket connections.");

            private static readonly Action<ILogger, Exception> _negotiationRequest =
                LoggerMessage.Define(LogLevel.Debug, new EventId(10, "NegotiationRequest"), "Sending negotiation response.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _receivedDeleteRequestForUnsupportedTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(11, "ReceivedDeleteRequestForUnsupportedTransport"), "Received DELETE request for unsupported transport: {TransportType}.");

            private static readonly Action<ILogger, Exception> _terminatingConnection =
                LoggerMessage.Define(LogLevel.Trace, new EventId(12, "TerminatingConection"), "Terminating Long Polling connection due to a DELETE request.");

            private static readonly Action<ILogger, string, Exception> _connectionDisposedWhileWriteInProgress =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, "ConnectionDisposedWhileWriteInProgress"), "Connection {TransportConnectionId} was disposed while a write was in progress.");

            private static readonly Action<ILogger, string, Exception> _failedToReadHttpRequestBody =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(14, "FailedToReadHttpRequestBody"), "Connection {TransportConnectionId} failed to read the HTTP request body.");

            public static void ConnectionDisposed(ILogger logger, string connectionId)
            {
                _connectionDisposed(logger, connectionId, null);
            }

            public static void ConnectionAlreadyActive(ILogger logger, string connectionId, string requestId)
            {
                _connectionAlreadyActive(logger, connectionId, requestId, null);
            }

            public static void PollCanceled(ILogger logger, string connectionId, string requestId)
            {
                _pollCanceled(logger, connectionId, requestId, null);
            }

            public static void EstablishedConnection(ILogger logger)
            {
                _establishedConnection(logger, null);
            }

            public static void ResumingConnection(ILogger logger)
            {
                _resumingConnection(logger, null);
            }

            public static void ReceivedBytes(ILogger logger, long count)
            {
                _receivedBytes(logger, count, null);
            }

            public static void TransportNotSupported(ILogger logger, HttpTransportType transport)
            {
                _transportNotSupported(logger, transport, null);
            }

            public static void CannotChangeTransport(ILogger logger, HttpTransportType transport, HttpTransportType requestTransport)
            {
                _cannotChangeTransport(logger, transport, requestTransport, null);
            }

            public static void PostNotAllowedForWebSockets(ILogger logger)
            {
                _postNotallowedForWebsockets(logger, null);
            }

            public static void NegotiationRequest(ILogger logger)
            {
                _negotiationRequest(logger, null);
            }

            public static void ReceivedDeleteRequestForUnsupportedTransport(ILogger logger, HttpTransportType transportType)
            {
                _receivedDeleteRequestForUnsupportedTransport(logger, transportType, null);
            }

            public static void TerminatingConection(ILogger logger)
            {
                _terminatingConnection(logger, null);
            }

            public static void ConnectionDisposedWhileWriteInProgress(ILogger logger, string connectionId, Exception ex)
            {
                _connectionDisposedWhileWriteInProgress(logger, connectionId, ex);
            }

            public static void FailedToReadHttpRequestBody(ILogger logger, string connectionId, Exception ex)
            {
                _failedToReadHttpRequestBody(logger, connectionId, ex);
            }
        }
    }
}

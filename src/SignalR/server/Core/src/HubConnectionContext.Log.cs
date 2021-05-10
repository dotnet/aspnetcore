// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public partial class HubConnectionContext
    {
        private static class Log
        {
            // Category: HubConnectionContext
            private static readonly Action<ILogger, string, Exception?> _handshakeComplete =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "HandshakeComplete"), "Completed connection handshake. Using HubProtocol '{Protocol}'.");

            private static readonly Action<ILogger, Exception?> _handshakeCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "HandshakeCanceled"), "Handshake was canceled.");

            private static readonly Action<ILogger, Exception?> _sentPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "SentPing"), "Sent a ping message to the client.");

            private static readonly Action<ILogger, Exception?> _transportBufferFull =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "TransportBufferFull"), "Unable to send Ping message to client, the transport buffer is full.");

            private static readonly Action<ILogger, Exception?> _handshakeFailed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "HandshakeFailed"), "Failed connection handshake.");

            private static readonly Action<ILogger, Exception> _failedWritingMessage =
                LoggerMessage.Define(LogLevel.Error, new EventId(6, "FailedWritingMessage"), "Failed writing message. Aborting connection.");

            private static readonly Action<ILogger, string, int, Exception?> _protocolVersionFailed =
                LoggerMessage.Define<string, int>(LogLevel.Debug, new EventId(7, "ProtocolVersionFailed"), "Server does not support version {Version} of the {Protocol} protocol.");

            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(8, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, int, Exception?> _clientTimeout =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(9, "ClientTimeout"), "Client timeout ({ClientTimeout}ms) elapsed without receiving a message from the client. Closing connection.");

            private static readonly Action<ILogger, long, Exception?> _handshakeSizeLimitExceeded =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(10, "HandshakeSizeLimitExceeded"), "The maximum message size of {MaxMessageSize}B was exceeded while parsing the Handshake. The message size can be configured in AddHubOptions.");

            public static void HandshakeComplete(ILogger logger, string hubProtocol)
            {
                _handshakeComplete(logger, hubProtocol, null);
            }

            public static void HandshakeCanceled(ILogger logger)
            {
                _handshakeCanceled(logger, null);
            }

            public static void SentPing(ILogger logger)
            {
                _sentPing(logger, null);
            }

            public static void TransportBufferFull(ILogger logger)
            {
                _transportBufferFull(logger, null);
            }

            public static void HandshakeFailed(ILogger logger, Exception? exception)
            {
                _handshakeFailed(logger, exception);
            }

            public static void FailedWritingMessage(ILogger logger, Exception exception)
            {
                _failedWritingMessage(logger, exception);
            }

            public static void ProtocolVersionFailed(ILogger logger, string protocolName, int version)
            {
                _protocolVersionFailed(logger, protocolName, version, null);
            }

            public static void AbortFailed(ILogger logger, Exception exception)
            {
                _abortFailed(logger, exception);
            }

            public static void ClientTimeout(ILogger logger, TimeSpan timeout)
            {
                _clientTimeout(logger, (int)timeout.TotalMilliseconds, null);
            }

            public static void HandshakeSizeLimitExceeded(ILogger logger, long maxMessageSize)
            {
                _handshakeSizeLimitExceeded(logger, maxMessageSize, null);
            }
        }
    }
}

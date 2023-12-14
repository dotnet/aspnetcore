// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed partial class HttpConnectionDispatcher
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Connection {TransportConnectionId} was disposed.", EventName = "ConnectionDisposed")]
        public static partial void ConnectionDisposed(ILogger logger, string transportConnectionId);

        [LoggerMessage(2, LogLevel.Debug, "Connection {TransportConnectionId} is already active via {RequestId}.", EventName = "ConnectionAlreadyActive")]
        public static partial void ConnectionAlreadyActive(ILogger logger, string transportConnectionId, string requestId);

        [LoggerMessage(3, LogLevel.Trace, "Previous poll canceled for {TransportConnectionId} on {RequestId}.", EventName = "PollCanceled")]
        public static partial void PollCanceled(ILogger logger, string transportConnectionId, string requestId);

        [LoggerMessage(4, LogLevel.Debug, "Establishing new connection.", EventName = "EstablishedConnection")]
        public static partial void EstablishedConnection(ILogger logger);

        [LoggerMessage(5, LogLevel.Debug, "Resuming existing connection.", EventName = "ResumingConnection")]
        public static partial void ResumingConnection(ILogger logger);

        [LoggerMessage(6, LogLevel.Trace, "Received {Count} bytes.", EventName = "ReceivedBytes")]
        public static partial void ReceivedBytes(ILogger logger, long count);

        [LoggerMessage(7, LogLevel.Debug, "{TransportType} transport not supported by this connection handler.", EventName = "TransportNotSupported")]
        public static partial void TransportNotSupported(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(8, LogLevel.Debug, "Cannot change transports mid-connection; currently using {TransportType}, requesting {RequestedTransport}.", EventName = "CannotChangeTransport")]
        public static partial void CannotChangeTransport(ILogger logger, HttpTransportType transportType, HttpTransportType requestedTransport);

        [LoggerMessage(9, LogLevel.Debug, "POST requests are not allowed for websocket connections.", EventName = "PostNotAllowedForWebSockets")]
        public static partial void PostNotAllowedForWebSockets(ILogger logger);

        [LoggerMessage(10, LogLevel.Debug, "Sending negotiation response.", EventName = "NegotiationRequest")]
        public static partial void NegotiationRequest(ILogger logger);

        [LoggerMessage(11, LogLevel.Trace, "Received DELETE request for unsupported transport: {TransportType}.", EventName = "ReceivedDeleteRequestForUnsupportedTransport")]
        public static partial void ReceivedDeleteRequestForUnsupportedTransport(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(12, LogLevel.Trace, "Terminating Long Polling connection due to a DELETE request.", EventName = "TerminatingConection")]
        public static partial void TerminatingConnection(ILogger logger);

        [LoggerMessage(13, LogLevel.Debug, "Connection {TransportConnectionId} was disposed while a write was in progress.", EventName = "ConnectionDisposedWhileWriteInProgress")]
        public static partial void ConnectionDisposedWhileWriteInProgress(ILogger logger, string transportConnectionId, Exception ex);

        [LoggerMessage(14, LogLevel.Debug, "Connection {TransportConnectionId} failed to read the HTTP request body.", EventName = "FailedToReadHttpRequestBody")]
        public static partial void FailedToReadHttpRequestBody(ILogger logger, string transportConnectionId, Exception ex);

        [LoggerMessage(15, LogLevel.Debug, "The client requested version '{clientProtocolVersion}', but the server does not support this version.", EventName = "NegotiateProtocolVersionMismatch")]
        public static partial void NegotiateProtocolVersionMismatch(ILogger logger, int clientProtocolVersion);

        [LoggerMessage(16, LogLevel.Debug, "The client requested an invalid protocol version '{queryStringVersionValue}'", EventName = "InvalidNegotiateProtocolVersion")]
        public static partial void InvalidNegotiateProtocolVersion(ILogger logger, string queryStringVersionValue);

        [LoggerMessage(17, LogLevel.Warning, "The name of the user changed from '{PreviousUserName}' to '{CurrentUserName}'.", EventName = "UserNameChanged")]
        private static partial void UserNameChangedInternal(ILogger logger, string previousUserName, string currentUserName);

        public static void UserNameChanged(ILogger logger, string? previousUserName, string? currentUserName)
        {
            UserNameChangedInternal(logger, previousUserName ?? "(null)", currentUserName ?? "(null)");
        }

        [LoggerMessage(18, LogLevel.Debug, "Exception from IStatefulReconnectFeature.NotifyOnReconnect callback.", EventName = "NotifyOnReconnectError")]
        public static partial void NotifyOnReconnectError(ILogger logger, Exception ex);
    }
}

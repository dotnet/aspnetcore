// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports;

internal sealed partial class WebSocketsServerTransport
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Socket opened using Sub-Protocol: '{SubProtocol}'.", EventName = "SocketOpened")]
        public static partial void SocketOpened(ILogger logger, string? subProtocol);

        [LoggerMessage(2, LogLevel.Debug, "Socket closed.", EventName = "SocketClosed")]
        public static partial void SocketClosed(ILogger logger);

        [LoggerMessage(3, LogLevel.Debug, "Client closed connection with status code '{Status}' ({Description}). Signaling end-of-input to application.", EventName = "ClientClosed")]
        public static partial void ClientClosed(ILogger logger, WebSocketCloseStatus? status, string description);

        [LoggerMessage(4, LogLevel.Debug, "Waiting for the application to finish sending data.", EventName = "WaitingForSend")]
        public static partial void WaitingForSend(ILogger logger);

        [LoggerMessage(5, LogLevel.Debug, "Application failed during sending. Sending InternalServerError close frame.", EventName = "FailedSending")]
        public static partial void FailedSending(ILogger logger);

        [LoggerMessage(6, LogLevel.Debug, "Application finished sending. Sending close frame.", EventName = "FinishedSending")]
        public static partial void FinishedSending(ILogger logger);

        [LoggerMessage(7, LogLevel.Debug, "Waiting for the client to close the socket.", EventName = "WaitingForClose")]
        public static partial void WaitingForClose(ILogger logger);

        [LoggerMessage(8, LogLevel.Debug, "Timed out waiting for client to send the close frame, aborting the connection.", EventName = "CloseTimedOut")]
        public static partial void CloseTimedOut(ILogger logger);

        [LoggerMessage(9, LogLevel.Trace, "Message received. Type: {MessageType}, size: {Size}, EndOfMessage: {EndOfMessage}.", EventName = "MessageReceived")]
        public static partial void MessageReceived(ILogger logger, WebSocketMessageType messageType, int size, bool endOfMessage);

        [LoggerMessage(10, LogLevel.Trace, "Passing message to application. Payload size: {Size}.", EventName = "MessageToApplication")]
        public static partial void MessageToApplication(ILogger logger, int size);

        [LoggerMessage(11, LogLevel.Trace, "Sending payload: {Size} bytes.", EventName = "SendPayload")]
        public static partial void SendPayload(ILogger logger, long size);

        [LoggerMessage(12, LogLevel.Debug, "Error writing frame.", EventName = "ErrorWritingFrame")]
        public static partial void ErrorWritingFrame(ILogger logger, Exception ex);

        [LoggerMessage(14, LogLevel.Debug, "Socket connection closed prematurely.", EventName = "ClosedPrematurely")]
        public static partial void ClosedPrematurely(ILogger logger, Exception ex);

        [LoggerMessage(15, LogLevel.Debug, "Closing webSocket failed.", EventName = "ClosingWebSocketFailed")]
        public static partial void ClosingWebSocketFailed(ILogger logger, Exception ex);

        [LoggerMessage(16, LogLevel.Debug, "Send loop errored.", EventName = "SendErrored")]
        public static partial void SendErrored(ILogger logger, Exception exception);
    }
}

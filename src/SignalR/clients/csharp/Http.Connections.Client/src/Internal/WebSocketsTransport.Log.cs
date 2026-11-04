// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class WebSocketsTransport
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Starting transport. Transfer mode: {TransferFormat}. Url: '{WebSocketUrl}'.", EventName = "StartTransport")]
        public static partial void StartTransport(ILogger logger, TransferFormat transferFormat, Uri webSocketUrl);

        [LoggerMessage(2, LogLevel.Debug, "Transport stopped.", EventName = "TransportStopped")]
        public static partial void TransportStopped(ILogger logger, Exception? exception);

        [LoggerMessage(3, LogLevel.Debug, "Starting receive loop.", EventName = "StartReceive")]
        public static partial void StartReceive(ILogger logger);

        [LoggerMessage(6, LogLevel.Information, "Transport is stopping.", EventName = "TransportStopping")]
        public static partial void TransportStopping(ILogger logger);

        [LoggerMessage(10, LogLevel.Debug, "Passing message to application. Payload size: {Count}.", EventName = "MessageToApp")]
        public static partial void MessageToApp(ILogger logger, int count);

        [LoggerMessage(5, LogLevel.Debug, "Receive loop canceled.", EventName = "ReceiveCanceled")]
        public static partial void ReceiveCanceled(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Receive loop stopped.", EventName = "ReceiveStopped")]
        public static partial void ReceiveStopped(ILogger logger);

        [LoggerMessage(7, LogLevel.Debug, "Starting the send loop.", EventName = "SendStarted")]
        public static partial void SendStarted(ILogger logger);

        [LoggerMessage(9, LogLevel.Debug, "Send loop canceled.", EventName = "SendCanceled")]
        public static partial void SendCanceled(ILogger logger);

        [LoggerMessage(8, LogLevel.Debug, "Send loop stopped.", EventName = "SendStopped")]
        public static partial void SendStopped(ILogger logger);

        [LoggerMessage(11, LogLevel.Information, "WebSocket closed by the server. Close status {CloseStatus}.", EventName = "WebSocketClosed")]
        public static partial void WebSocketClosed(ILogger logger, WebSocketCloseStatus? closeStatus);

        [LoggerMessage(12, LogLevel.Debug, "Message received. Type: {MessageType}, size: {Count}, EndOfMessage: {EndOfMessage}.", EventName = "MessageReceived")]
        public static partial void MessageReceived(ILogger logger, WebSocketMessageType messageType, int count, bool endOfMessage);

        [LoggerMessage(13, LogLevel.Debug, "Received message from application. Payload size: {Count}.", EventName = "ReceivedFromApp")]
        public static partial void ReceivedFromApp(ILogger logger, long count);

        [LoggerMessage(14, LogLevel.Information, "Sending a message canceled.", EventName = "SendMessageCanceled")]
        public static partial void SendMessageCanceled(ILogger logger);

        [LoggerMessage(15, LogLevel.Error, "Error while sending a message.", EventName = "ErrorSendingMessage")]
        public static partial void ErrorSendingMessage(ILogger logger, Exception exception);

        [LoggerMessage(16, LogLevel.Information, "Closing WebSocket.", EventName = "ClosingWebSocket")]
        public static partial void ClosingWebSocket(ILogger logger);

        [LoggerMessage(17, LogLevel.Debug, "Closing webSocket failed.", EventName = "ClosingWebSocketFailed")]
        public static partial void ClosingWebSocketFailed(ILogger logger, Exception exception);

        [LoggerMessage(18, LogLevel.Debug, "Canceled passing message to application.", EventName = "CancelMessage")]
        public static partial void CancelMessage(ILogger logger);

        [LoggerMessage(19, LogLevel.Debug, "Started transport.", EventName = "StartedTransport")]
        public static partial void StartedTransport(ILogger logger);

        [LoggerMessage(20, LogLevel.Warning, $"Configuring request headers using {nameof(HttpConnectionOptions)}.{nameof(HttpConnectionOptions.Headers)} is not supported when using websockets transport " +
                "on the browser platform.", EventName = "HeadersNotSupported")]
        public static partial void HeadersNotSupported(ILogger logger);

        [LoggerMessage(21, LogLevel.Debug, "Receive loop errored.", EventName = "ReceiveErrored")]
        public static partial void ReceiveErrored(ILogger logger, Exception exception);

        [LoggerMessage(22, LogLevel.Debug, "Send loop errored.", EventName = "SendErrored")]
        public static partial void SendErrored(ILogger logger, Exception exception);
    }
}

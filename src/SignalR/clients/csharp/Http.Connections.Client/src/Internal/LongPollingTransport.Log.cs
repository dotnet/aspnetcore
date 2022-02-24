// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal partial class LongPollingTransport
{
    // EventIds 100 - 106 used in SendUtils
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Starting transport. Transfer mode: {TransferFormat}.", EventName = "StartTransport")]
        public static partial void StartTransport(ILogger logger, TransferFormat transferFormat);

        [LoggerMessage(2, LogLevel.Debug, "Transport stopped.", EventName = "TransportStopped")]
        public static partial void TransportStopped(ILogger logger, Exception? exception);

        [LoggerMessage(3, LogLevel.Debug, "Starting receive loop.", EventName = "StartReceive")]
        public static partial void StartReceive(ILogger logger);

        [LoggerMessage(6, LogLevel.Information, "Transport is stopping.", EventName = "TransportStopping")]
        public static partial void TransportStopping(ILogger logger);

        [LoggerMessage(5, LogLevel.Debug, "Receive loop canceled.", EventName = "ReceiveCanceled")]
        public static partial void ReceiveCanceled(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Receive loop stopped.", EventName = "ReceiveStopped")]
        public static partial void ReceiveStopped(ILogger logger);

        [LoggerMessage(7, LogLevel.Debug, "The server is closing the connection.", EventName = "ClosingConnection")]
        public static partial void ClosingConnection(ILogger logger);

        [LoggerMessage(8, LogLevel.Debug, "Received messages from the server.", EventName = "ReceivedMessages")]
        public static partial void ReceivedMessages(ILogger logger);

        [LoggerMessage(9, LogLevel.Error, "Error while polling '{PollUrl}'.", EventName = "ErrorPolling")]
        public static partial void ErrorPolling(ILogger logger, Uri pollUrl, Exception exception);

        // long? does properly format as "(null)" when null.
        public static void PollResponseReceived(ILogger logger, HttpResponseMessage response)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                PollResponseReceived(logger, (int)response.StatusCode,
                    response.Content.Headers.ContentLength ?? -1);
            }
        }

        [LoggerMessage(10, LogLevel.Trace, "Poll response with status code {StatusCode} received from server. Content length: {ContentLength}.", EventName = "PollResponseReceived", SkipEnabledCheck = true)]
        private static partial void PollResponseReceived(ILogger logger, int statusCode, long contentLength);

        [LoggerMessage(11, LogLevel.Debug, "Sending DELETE request to '{PollUrl}'.", EventName = "SendingDeleteRequest")]
        public static partial void SendingDeleteRequest(ILogger logger, Uri pollUrl);

        [LoggerMessage(12, LogLevel.Debug, "DELETE request to '{PollUrl}' accepted.", EventName = "DeleteRequestAccepted")]
        public static partial void DeleteRequestAccepted(ILogger logger, Uri pollUrl);

        [LoggerMessage(13, LogLevel.Error, "Error sending DELETE request to '{PollUrl}'.", EventName = "ErrorSendingDeleteRequest")]
        public static partial void ErrorSendingDeleteRequest(ILogger logger, Uri pollUrl, Exception ex);

        [LoggerMessage(14, LogLevel.Debug, "A 404 response was returned from sending DELETE request to '{PollUrl}', likely because the transport was already closed on the server.", EventName = "ConnectionAlreadyClosedSendingDeleteRequest")]
        public static partial void ConnectionAlreadyClosedSendingDeleteRequest(ILogger logger, Uri pollUrl);
    }
}

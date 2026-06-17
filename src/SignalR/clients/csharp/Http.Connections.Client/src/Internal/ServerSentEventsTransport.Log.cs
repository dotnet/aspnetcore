// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class ServerSentEventsTransport
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

        [LoggerMessage(7, LogLevel.Debug, "Passing message to application. Payload size: {Count}.", EventName = "MessageToApplication")]
        public static partial void MessageToApplication(ILogger logger, int count);

        [LoggerMessage(5, LogLevel.Debug, "Receive loop canceled.", EventName = "ReceiveCanceled")]
        public static partial void ReceiveCanceled(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Receive loop stopped.", EventName = "ReceiveStopped")]
        public static partial void ReceiveStopped(ILogger logger);

        [LoggerMessage(8, LogLevel.Debug, "Server-Sent Event Stream ended.", EventName = "EventStreamEnded")]
        public static partial void EventStreamEnded(ILogger logger);

        // No longer used
        [LoggerMessage(9, LogLevel.Debug, "Received {Count} bytes. Parsing SSE frame.", EventName = "ParsingSSE")]
        public static partial void ParsingSSE(ILogger logger, long count);
    }
}

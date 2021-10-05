// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal static partial class LibuvTrace
    {
        public static void ConnectionRead(ILogger logger, string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" received FIN.", EventName = nameof(ConnectionReadFin))]
        public static partial void ConnectionReadFin(ILogger logger, string connectionId);

        [LoggerMessage(7, LogLevel.Debug, @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""", EventName = nameof(ConnectionWriteFin))]
        public static partial void ConnectionWriteFin(ILogger logger, string connectionId, string reason);

        public static void ConnectionWrite(ILogger logger, string connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public static void ConnectionWriteCallback(ILogger logger, string connectionId, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        [LoggerMessage(14, LogLevel.Debug, @"Connection id ""{ConnectionId}"" communication error.", EventName = nameof(ConnectionError))]
        public static partial void ConnectionError(ILogger logger, string connectionId, Exception ex);

        [LoggerMessage(19, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reset.", EventName = nameof(ConnectionReset))]
        public static partial void ConnectionReset(ILogger logger, string connectionId);

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = nameof(ConnectionPause))]
        public static partial void ConnectionPause(ILogger logger, string connectionId);

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = nameof(ConnectionResume))]
        public static partial void ConnectionResume(ILogger logger, string connectionId);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed partial class HttpConnectionManager
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "New connection {TransportConnectionId} created.", EventName = "CreatedNewConnection")]
        public static partial void CreatedNewConnection(ILogger logger, string transportConnectionId);

        [LoggerMessage(2, LogLevel.Debug, "Removing connection {TransportConnectionId} from the list of connections.", EventName = "RemovedConnection")]
        public static partial void RemovedConnection(ILogger logger, string transportConnectionId);

        [LoggerMessage(3, LogLevel.Error, "Failed disposing connection {TransportConnectionId}.", EventName = "FailedDispose")]
        public static partial void FailedDispose(ILogger logger, string transportConnectionId, Exception exception);

        [LoggerMessage(5, LogLevel.Trace, "Connection {TransportConnectionId} timed out.", EventName = "ConnectionTimedOut")]
        public static partial void ConnectionTimedOut(ILogger logger, string transportConnectionId);

        [LoggerMessage(4, LogLevel.Trace, "Connection {TransportConnectionId} was reset.", EventName = "ConnectionReset")]
        public static partial void ConnectionReset(ILogger logger, string transportConnectionId, Exception exception);

        [LoggerMessage(7, LogLevel.Error, "Scanning connections failed.", EventName = "ScanningConnectionsFailed")]
        public static partial void ScanningConnectionsFailed(ILogger logger, Exception exception);

        // 8, ScannedConnections - removed

        [LoggerMessage(9, LogLevel.Trace, "Starting connection heartbeat.", EventName = "HeartBeatStarted")]
        public static partial void HeartBeatStarted(ILogger logger);

        [LoggerMessage(10, LogLevel.Trace, "Ending connection heartbeat.", EventName = "HeartBeatEnded")]
        public static partial void HeartBeatEnded(ILogger logger);

        [LoggerMessage(11, LogLevel.Debug, "Connection {TransportConnectionId} closing because the authentication token has expired.", EventName = "AuthenticationExpired")]
        public static partial void AuthenticationExpired(ILogger logger, string transportConnectionId);
    }
}

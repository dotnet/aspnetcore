// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void ConnectionStart(string connectionId)
    {
        ConnectionsLog.ConnectionStart(_connectionsLogger, connectionId);
    }

    public void ConnectionStop(string connectionId)
    {
        ConnectionsLog.ConnectionStop(_connectionsLogger, connectionId);
    }

    public void ConnectionPause(string connectionId)
    {
        ConnectionsLog.ConnectionPause(_connectionsLogger, connectionId);
    }

    public void ConnectionResume(string connectionId)
    {
        ConnectionsLog.ConnectionResume(_connectionsLogger, connectionId);
    }

    public void ConnectionKeepAlive(string connectionId)
    {
        ConnectionsLog.ConnectionKeepAlive(_connectionsLogger, connectionId);
    }

    public void ConnectionDisconnect(string connectionId)
    {
        ConnectionsLog.ConnectionDisconnect(_connectionsLogger, connectionId);
    }

    public void NotAllConnectionsClosedGracefully()
    {
        ConnectionsLog.NotAllConnectionsClosedGracefully(_connectionsLogger);
    }

    public void NotAllConnectionsAborted()
    {
        ConnectionsLog.NotAllConnectionsAborted(_connectionsLogger);
    }

    public void ConnectionRejected(string connectionId)
    {
        ConnectionsLog.ConnectionRejected(_connectionsLogger, connectionId);
    }

    public void ApplicationAbortedConnection(string connectionId, string traceIdentifier)
    {
        ConnectionsLog.ApplicationAbortedConnection(_connectionsLogger, connectionId, traceIdentifier);
    }

    public void ConnectionAccepted(string connectionId)
    {
        ConnectionsLog.ConnectionAccepted(_connectionsLogger, connectionId);
    }

    private static partial class ConnectionsLog
    {
        [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" started.", EventName = "ConnectionStart")]
        public static partial void ConnectionStart(ILogger logger, string connectionId);

        [LoggerMessage(2, LogLevel.Debug, @"Connection id ""{ConnectionId}"" stopped.", EventName = "ConnectionStop")]
        public static partial void ConnectionStop(ILogger logger, string connectionId);

        [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = "ConnectionPause")]
        public static partial void ConnectionPause(ILogger logger, string connectionId);

        [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = "ConnectionResume")]
        public static partial void ConnectionResume(ILogger logger, string connectionId);

        [LoggerMessage(9, LogLevel.Debug, @"Connection id ""{ConnectionId}"" completed keep alive response.", EventName = "ConnectionKeepAlive")]
        public static partial void ConnectionKeepAlive(ILogger logger, string connectionId);

        [LoggerMessage(10, LogLevel.Debug, @"Connection id ""{ConnectionId}"" disconnecting.", EventName = "ConnectionDisconnect")]
        public static partial void ConnectionDisconnect(ILogger logger, string connectionId);

        [LoggerMessage(16, LogLevel.Debug, "Some connections failed to close gracefully during server shutdown.", EventName = "NotAllConnectionsClosedGracefully")]
        public static partial void NotAllConnectionsClosedGracefully(ILogger logger);

        [LoggerMessage(21, LogLevel.Debug, "Some connections failed to abort during server shutdown.", EventName = "NotAllConnectionsAborted")]
        public static partial void NotAllConnectionsAborted(ILogger logger);

        [LoggerMessage(24, LogLevel.Warning, @"Connection id ""{ConnectionId}"" rejected because the maximum number of concurrent connections has been reached.", EventName = "ConnectionRejected")]
        public static partial void ConnectionRejected(ILogger logger, string connectionId);

        [LoggerMessage(34, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application aborted the connection.", EventName = "ApplicationAbortedConnection")]
        public static partial void ApplicationAbortedConnection(ILogger logger, string connectionId, string traceIdentifier);

        [LoggerMessage(39, LogLevel.Debug, @"Connection id ""{ConnectionId}"" accepted.", EventName = "ConnectionAccepted")]
        public static partial void ConnectionAccepted(ILogger logger, string connectionId);

        // IDs prior to 64 are reserved for back compat (the various KestrelTrace loggers used to share a single sequence)
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;

internal static partial class NamedPipeLog
{
    [LoggerMessage(1, LogLevel.Debug, @"Connection id ""{ConnectionId}"" accepted.", EventName = "AcceptedConnection", SkipEnabledCheck = true)]
    private static partial void AcceptedConnectionCore(ILogger logger, string connectionId);

    public static void AcceptedConnection(ILogger logger, BaseConnectionContext connection)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            AcceptedConnectionCore(logger, connection.ConnectionId);
        }
    }

    [LoggerMessage(2, LogLevel.Debug, @"Connection id ""{ConnectionId}"" unexpected error.", EventName = "ConnectionError", SkipEnabledCheck = true)]
    private static partial void ConnectionErrorCore(ILogger logger, string connectionId, Exception ex);

    public static void ConnectionError(ILogger logger, BaseConnectionContext connection, Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionErrorCore(logger, connection.ConnectionId, ex);
        }
    }

    [LoggerMessage(3, LogLevel.Error, "Named pipe listener aborted.", EventName = "ConnectionListenerAborted")]
    public static partial void ConnectionListenerAborted(ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = "ConnectionPause", SkipEnabledCheck = true)]
    private static partial void ConnectionPauseCore(ILogger logger, string connectionId);

    public static void ConnectionPause(ILogger logger, NamedPipeConnection connection)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionPauseCore(logger, connection.ConnectionId);
        }
    }

    [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = "ConnectionResume", SkipEnabledCheck = true)]
    private static partial void ConnectionResumeCore(ILogger logger, string connectionId);

    public static void ConnectionResume(ILogger logger, NamedPipeConnection connection)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionResumeCore(logger, connection.ConnectionId);
        }
    }

    [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" received end of stream.", EventName = "ConnectionReadEnd", SkipEnabledCheck = true)]
    private static partial void ConnectionReadEndCore(ILogger logger, string connectionId);

    public static void ConnectionReadEnd(ILogger logger, NamedPipeConnection connection)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionReadEndCore(logger, connection.ConnectionId);
        }
    }

    [LoggerMessage(7, LogLevel.Debug, @"Connection id ""{ConnectionId}"" disconnecting stream because: ""{Reason}""", EventName = "ConnectionDisconnect", SkipEnabledCheck = true)]
    private static partial void ConnectionDisconnectCore(ILogger logger, string connectionId, string reason);

    public static void ConnectionDisconnect(ILogger logger, NamedPipeConnection connection, string reason)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionDisconnectCore(logger, connection.ConnectionId, reason);
        }
    }

    [LoggerMessage(8, LogLevel.Debug, "Named pipe listener received broken pipe while waiting for a connection.", EventName = "ConnectionListenerBrokenPipe")]
    public static partial void ConnectionListenerBrokenPipe(ILogger logger, Exception ex);

    [LoggerMessage(9, LogLevel.Trace, "Named pipe listener queue exited.", EventName = "ConnectionListenerQueueExited")]
    public static partial void ConnectionListenerQueueExited(ILogger logger);
}

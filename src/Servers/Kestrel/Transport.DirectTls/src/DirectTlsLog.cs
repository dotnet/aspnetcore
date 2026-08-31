// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

// Mirrors SocketsLog events:
// https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketsLog.cs
internal static partial class DirectTlsLog
{
    [LoggerMessage(4, LogLevel.Debug, @"Connection id ""{ConnectionId}"" paused.", EventName = "ConnectionPause", SkipEnabledCheck = true)]
    private static partial void ConnectionPauseCore(ILogger logger, string connectionId);

    public static void ConnectionPause(ILogger logger, string connectionId)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionPauseCore(logger, connectionId);
        }
    }

    [LoggerMessage(5, LogLevel.Debug, @"Connection id ""{ConnectionId}"" resumed.", EventName = "ConnectionResume", SkipEnabledCheck = true)]
    private static partial void ConnectionResumeCore(ILogger logger, string connectionId);

    public static void ConnectionResume(ILogger logger, string connectionId)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionResumeCore(logger, connectionId);
        }
    }

    [LoggerMessage(6, LogLevel.Debug, @"Connection id ""{ConnectionId}"" received FIN.", EventName = "ConnectionReadFin", SkipEnabledCheck = true)]
    private static partial void ConnectionReadFinCore(ILogger logger, string connectionId);

    public static void ConnectionReadFin(ILogger logger, string connectionId)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionReadFinCore(logger, connectionId);
        }
    }

    [LoggerMessage(7, LogLevel.Debug, @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""", EventName = "ConnectionWriteFin", SkipEnabledCheck = true)]
    private static partial void ConnectionWriteFinCore(ILogger logger, string connectionId, string reason);

    public static void ConnectionWriteFin(ILogger logger, string connectionId, string reason)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionWriteFinCore(logger, connectionId, reason);
        }
    }

    [LoggerMessage(8, LogLevel.Debug, @"Connection id ""{ConnectionId}"" sending RST because: ""{Reason}""", EventName = "ConnectionWriteRst", SkipEnabledCheck = true)]
    private static partial void ConnectionWriteRstCore(ILogger logger, string connectionId, string reason);

    public static void ConnectionWriteRst(ILogger logger, string connectionId, string reason)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionWriteRstCore(logger, connectionId, reason);
        }
    }

    [LoggerMessage(14, LogLevel.Debug, @"Connection id ""{ConnectionId}"" communication error.", EventName = "ConnectionError", SkipEnabledCheck = true)]
    private static partial void ConnectionErrorCore(ILogger logger, string connectionId, Exception exception);

    public static void ConnectionError(ILogger logger, string connectionId, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionErrorCore(logger, connectionId, exception);
        }
    }

    [LoggerMessage(19, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reset.", EventName = "ConnectionReset", SkipEnabledCheck = true)]
    private static partial void ConnectionResetCore(ILogger logger, string connectionId);

    public static void ConnectionReset(ILogger logger, string connectionId)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            ConnectionResetCore(logger, connectionId);
        }
    }
}

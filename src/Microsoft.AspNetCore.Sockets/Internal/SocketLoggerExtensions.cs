// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    internal static class SocketLoggerExtensions
    {
        // Category: ConnectionManager
        private static readonly Action<ILogger, DateTime, string, Exception> _createdNewConnection =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(0, nameof(CreatedNewConnection)), "{time}: ConnectionId {connectionId}: New connection created.");

        private static readonly Action<ILogger, DateTime, string, Exception> _removedConnection =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, new EventId(1, nameof(RemovedConnection)), "{time}: ConnectionId {connectionId}: Removing connection from the list of connections.");

        private static readonly Action<ILogger, DateTime, string, Exception> _failedDispose =
            LoggerMessage.Define<DateTime, string>(LogLevel.Error, new EventId(2, nameof(FailedDispose)), "{time}: ConnectionId {connectionId}: Failed disposing connection.");

        private static readonly Action<ILogger, DateTime, string, Exception> _connectionReset =
            LoggerMessage.Define<DateTime, string>(LogLevel.Trace, new EventId(3, nameof(ConnectionReset)), "{time}: ConnectionId {connectionId}: Connection was reset.");

        private static readonly Action<ILogger, DateTime, string, Exception> _connectionTimedOut =
            LoggerMessage.Define<DateTime, string>(LogLevel.Trace, new EventId(4, nameof(ConnectionTimedOut)), "{time}: ConnectionId {connectionId}: Connection timed out.");

        private static readonly Action<ILogger, DateTime, Exception> _scanningConnections =
            LoggerMessage.Define<DateTime>(LogLevel.Trace, new EventId(5, nameof(ScanningConnections)), "{time}: Scanning connections.");

        private static readonly Action<ILogger, DateTime, TimeSpan, Exception> _scannedConnections =
            LoggerMessage.Define<DateTime, TimeSpan>(LogLevel.Trace, new EventId(6, nameof(ScannedConnections)), "{time}: Scanned connections in {duration}.");

        public static void CreatedNewConnection(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _createdNewConnection(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void RemovedConnection(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                _removedConnection(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void FailedDispose(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                _failedDispose(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void ConnectionTimedOut(this ILogger logger, string connectionId)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _connectionTimedOut(logger, DateTime.Now, connectionId, null);
            }
        }

        public static void ConnectionReset(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _connectionReset(logger, DateTime.Now, connectionId, exception);
            }
        }

        public static void ScanningConnections(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _scanningConnections(logger, DateTime.Now, null);
            }
        }

        public static void ScannedConnections(this ILogger logger, TimeSpan duration)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _scannedConnections(logger, DateTime.Now, duration, null);
            }
        }
    }
}

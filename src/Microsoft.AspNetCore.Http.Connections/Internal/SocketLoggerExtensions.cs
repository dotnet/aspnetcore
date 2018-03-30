// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal static class SocketLoggerExtensions
    {
        // Category: ConnectionManager
        private static readonly Action<ILogger, string, Exception> _createdNewConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(CreatedNewConnection)), "New connection {TransportConnectionId} created.");

        private static readonly Action<ILogger, string, Exception> _removedConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, nameof(RemovedConnection)), "Removing connection {TransportConnectionId} from the list of connections.");

        private static readonly Action<ILogger, string, Exception> _failedDispose =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, nameof(FailedDispose)), "Failed disposing connection {TransportConnectionId}.");

        private static readonly Action<ILogger, string, Exception> _connectionReset =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, nameof(ConnectionReset)), "Connection {TransportConnectionId} was reset.");

        private static readonly Action<ILogger, string, Exception> _connectionTimedOut =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, nameof(ConnectionTimedOut)), "Connection {TransportConnectionId} timed out.");

        private static readonly Action<ILogger, Exception> _scanningConnections =
            LoggerMessage.Define(LogLevel.Trace, new EventId(6, nameof(ScanningConnections)), "Scanning connections.");

        private static readonly Action<ILogger, TimeSpan, Exception> _scannedConnections =
            LoggerMessage.Define<TimeSpan>(LogLevel.Trace, new EventId(7, nameof(ScannedConnections)), "Scanned connections in {Duration}.");

        public static void CreatedNewConnection(this ILogger logger, string connectionId)
        {
            _createdNewConnection(logger, connectionId, null);
        }

        public static void RemovedConnection(this ILogger logger, string connectionId)
        {
            _removedConnection(logger, connectionId, null);
        }

        public static void FailedDispose(this ILogger logger, string connectionId, Exception exception)
        {
            _failedDispose(logger, connectionId, exception);
        }

        public static void ConnectionTimedOut(this ILogger logger, string connectionId)
        {
            _connectionTimedOut(logger, connectionId, null);
        }

        public static void ConnectionReset(this ILogger logger, string connectionId, Exception exception)
        {
            _connectionReset(logger, connectionId, exception);
        }

        public static void ScanningConnections(this ILogger logger)
        {
            _scanningConnections(logger, null);
        }

        public static void ScannedConnections(this ILogger logger, TimeSpan duration)
        {
            _scannedConnections(logger, duration, null);
        }
    }
}

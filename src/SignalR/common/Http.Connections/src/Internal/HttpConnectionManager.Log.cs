// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal partial class HttpConnectionManager
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _createdNewConnection =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "CreatedNewConnection"), "New connection {TransportConnectionId} created.");

            private static readonly Action<ILogger, string, Exception> _removedConnection =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "RemovedConnection"), "Removing connection {TransportConnectionId} from the list of connections.");

            private static readonly Action<ILogger, string, Exception> _failedDispose =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "FailedDispose"), "Failed disposing connection {TransportConnectionId}.");

            private static readonly Action<ILogger, string, Exception> _connectionReset =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, "ConnectionReset"), "Connection {TransportConnectionId} was reset.");

            private static readonly Action<ILogger, string, Exception> _connectionTimedOut =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "ConnectionTimedOut"), "Connection {TransportConnectionId} timed out.");

            // 6, ScanningConnections - removed

            private static readonly Action<ILogger, Exception> _scanningConnectionsFailed =
                LoggerMessage.Define(LogLevel.Error, new EventId(7, "ScanningConnectionsFailed"), "Scanning connections failed.");

            // 8, ScannedConnections - removed

            private static readonly Action<ILogger, Exception> _heartbeatStarted =
                LoggerMessage.Define(LogLevel.Trace, new EventId(9, "HeartBeatStarted"), "Starting connection heartbeat.");

            private static readonly Action<ILogger, Exception> _heartbeatEnded =
                LoggerMessage.Define(LogLevel.Trace, new EventId(10, "HeartBeatEnded"), "Ending connection heartbeat.");

            public static void CreatedNewConnection(ILogger logger, string connectionId)
            {
                _createdNewConnection(logger, connectionId, null);
            }

            public static void RemovedConnection(ILogger logger, string connectionId)
            {
                _removedConnection(logger, connectionId, null);
            }

            public static void FailedDispose(ILogger logger, string connectionId, Exception exception)
            {
                _failedDispose(logger, connectionId, exception);
            }

            public static void ConnectionTimedOut(ILogger logger, string connectionId)
            {
                _connectionTimedOut(logger, connectionId, null);
            }

            public static void ConnectionReset(ILogger logger, string connectionId, Exception exception)
            {
                _connectionReset(logger, connectionId, exception);
            }

            public static void ScanningConnectionsFailed(ILogger logger, Exception exception)
            {
                _scanningConnectionsFailed(logger, exception);
            }

            public static void HeartBeatStarted(ILogger logger)
            {
                _heartbeatStarted(logger, null);
            }

            public static void HeartBeatEnded(ILogger logger)
            {
                _heartbeatEnded(logger, null);
            }
        }
    }
}

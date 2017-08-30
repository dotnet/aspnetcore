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
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, 0, "{time}: ConnectionId {connectionId}: New connection created.");

        private static readonly Action<ILogger, DateTime, string, Exception> _removedConnection =
            LoggerMessage.Define<DateTime, string>(LogLevel.Debug, 1, "{time}: ConnectionId {connectionId}: Removing connection from the list of connections.");

        private static readonly Action<ILogger, DateTime, string, Exception> _failedDispose =
            LoggerMessage.Define<DateTime, string>(LogLevel.Error, 2, "{time}: ConnectionId {connectionId}: Failed disposing connection.");

        private static readonly Action<ILogger, DateTime, string, Exception> _connectionReset =
            LoggerMessage.Define<DateTime, string>(LogLevel.Trace, 3, "{time}: ConnectionId {connectionId}: Connection was reset.");

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

        public static void ConnectionReset(this ILogger logger, string connectionId, Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _connectionReset(logger, DateTime.Now, connectionId, exception);
            }
        }
    }
}

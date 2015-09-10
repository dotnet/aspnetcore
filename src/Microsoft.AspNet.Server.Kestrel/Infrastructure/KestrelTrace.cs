// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelTrace
    /// </summary>
    public class KestrelTrace : IKestrelTrace
    {
        private readonly ILogger _logger;

        public KestrelTrace(ILogger logger)
        {
            _logger = logger;
        }

        public void ConnectionStart(long connectionId)
        {
            _logger.LogDebug(1, @"Connection id ""{ConnectionId}"" started.", connectionId);
        }

        public void ConnectionStop(long connectionId)
        {
            _logger.LogDebug(2, @"Connection id ""{ConnectionId}"" stopped.", connectionId);
        }

        public void ConnectionRead(long connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        public void ConnectionPause(long connectionId)
        {
            _logger.LogDebug(4, @"Connection id ""{ConnectionId}"" paused.", connectionId);
        }

        public void ConnectionResume(long connectionId)
        {
            _logger.LogDebug(5, @"Connection id ""{ConnectionId}"" resumed.", connectionId);
        }

        public void ConnectionReadFin(long connectionId)
        {
            _logger.LogDebug(6, @"Connection id ""{ConnectionId}"" received FIN.", connectionId);
        }

        public void ConnectionWriteFin(long connectionId)
        {
            _logger.LogDebug(7, @"Connection id ""{ConnectionId}"" sending FIN.");
        }

        public void ConnectionWroteFin(long connectionId, int status)
        {
            _logger.LogDebug(8, @"Connection id ""{ConnectionId}"" sent FIN with status ""{Status}"".", status);
        }

        public void ConnectionKeepAlive(long connectionId)
        {
            _logger.LogDebug(9, @"Connection id ""{ConnectionId}"" completed keep alive response.", connectionId);
        }

        public void ConnectionDisconnect(long connectionId)
        {
            _logger.LogDebug(10, @"Connection id ""{ConnectionId}"" disconnected.", connectionId);
        }

        public void ConnectionWrite(long connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public void ConnectionWriteCallback(long connectionId, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return _logger.BeginScopeImpl(state);
        }
    }
}
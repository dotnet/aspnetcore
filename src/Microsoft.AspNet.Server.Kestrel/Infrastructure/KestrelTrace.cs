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
        protected readonly ILogger _logger;

        public KestrelTrace(ILogger logger)
        {
            _logger = logger;
        }

        public virtual void ConnectionStart(long connectionId)
        {
            _logger.LogDebug(1, @"Connection id ""{ConnectionId}"" started.", connectionId);
        }

        public virtual void ConnectionStop(long connectionId)
        {
            _logger.LogDebug(2, @"Connection id ""{ConnectionId}"" stopped.", connectionId);
        }

        public virtual void ConnectionRead(long connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }

        public virtual void ConnectionPause(long connectionId)
        {
            _logger.LogDebug(4, @"Connection id ""{ConnectionId}"" paused.", connectionId);
        }

        public virtual void ConnectionResume(long connectionId)
        {
            _logger.LogDebug(5, @"Connection id ""{ConnectionId}"" resumed.", connectionId);
        }

        public virtual void ConnectionReadFin(long connectionId)
        {
            _logger.LogDebug(6, @"Connection id ""{ConnectionId}"" received FIN.", connectionId);
        }

        public virtual void ConnectionWriteFin(long connectionId)
        {
            _logger.LogDebug(7, @"Connection id ""{ConnectionId}"" sending FIN.", connectionId);
        }

        public virtual void ConnectionWroteFin(long connectionId, int status)
        {
            _logger.LogDebug(8, @"Connection id ""{ConnectionId}"" sent FIN with status ""{Status}"".", connectionId, status);
        }

        public virtual void ConnectionKeepAlive(long connectionId)
        {
            _logger.LogDebug(9, @"Connection id ""{ConnectionId}"" completed keep alive response.", connectionId);
        }

        public virtual void ConnectionDisconnect(long connectionId)
        {
            _logger.LogDebug(10, @"Connection id ""{ConnectionId}"" disconnected.", connectionId);
        }

        public virtual void ConnectionWrite(long connectionId, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }

        public virtual void ConnectionWriteCallback(long connectionId, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }

        public virtual void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public virtual IDisposable BeginScopeImpl(object state)
        {
            return _logger.BeginScopeImpl(state);
        }
    }
}
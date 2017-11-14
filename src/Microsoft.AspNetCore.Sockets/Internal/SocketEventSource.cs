// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    [EventSource(Name = "Microsoft-AspNetCore-Sockets")]
    internal class SocketEventSource : EventSource
    {
        public static readonly SocketEventSource Log = new SocketEventSource();

        private readonly EventCounter _connectionsStarted;
        private readonly EventCounter _connectionsStopped;
        private readonly EventCounter _connectionsTimedOut;
        private readonly EventCounter _connectionDuration;

        private SocketEventSource()
        {
            _connectionsStarted = new EventCounter("ConnectionsStarted", this);
            _connectionsStopped = new EventCounter("ConnectionsStopped", this);
            _connectionsTimedOut = new EventCounter("ConnectionsTimedOut", this);
            _connectionDuration = new EventCounter("ConnectionDuration", this);
        }

        // This has to go through NonEvent because only Primitive types are allowed
        // in function parameters for Events
        [NonEvent]
        public void ConnectionStop(string connectionId, ValueStopwatch timer)
        {
            if (IsEnabled())
            {
                var duration = timer.IsActive ? timer.GetElapsedTime().TotalMilliseconds : 0.0;
                _connectionDuration.WriteMetric((float)duration);
                _connectionsStopped.WriteMetric(1.0f);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    ConnectionStop(connectionId);
                }
            }
        }

        [Event(eventId: 1, Level = EventLevel.Informational, Message = "Started connection '{0}'.")]
        public ValueStopwatch ConnectionStart(string connectionId)
        {
            if (IsEnabled())
            {
                _connectionsStarted.WriteMetric(1.0f);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    WriteEvent(1, connectionId);
                    return ValueStopwatch.StartNew();
                }
            }
            return default;
        }

        [Event(eventId: 2, Level = EventLevel.Informational, Message = "Stopped connection '{0}'.")]
        private void ConnectionStop(string connectionId) => WriteEvent(2, connectionId);

        [Event(eventId: 3, Level = EventLevel.Informational, Message = "Connection '{0}' timed out.")]
        public void ConnectionTimedOut(string connectionId)
        {
            if (IsEnabled())
            {
                _connectionsTimedOut.WriteMetric(1.0f);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    WriteEvent(3, connectionId);
                }
            }
        }
    }
}

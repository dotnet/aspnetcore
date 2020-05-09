// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    [EventSource(Name = "Microsoft-AspNetCore-Server-Kestrel")]
    internal sealed class KestrelEventSource : EventSource
    {
        public static readonly KestrelEventSource Log = new KestrelEventSource();

        private IncrementingPollingCounter _connectionsPerSecondCounter;
        private PollingCounter _totalConnectionsCounter;
        private PollingCounter _currentConnectionsCounter;
        private PollingCounter _connectionQueueLengthCounter;

        private long _totalConnections;
        private long _currentConnections;
        private long _connectionQueueLength;

        private KestrelEventSource()
        {
        }

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.
        // - Avoid renaming methods or parameters marked with EventAttribute. EventSource uses these to form the event object.

        [NonEvent]
        public void ConnectionStart(BaseConnectionContext connection)
        {
            // avoid allocating strings unless this event source is enabled
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _currentConnections);

            if (IsEnabled())
            {
                ConnectionStart(
                    connection.ConnectionId,
                    connection.LocalEndPoint?.ToString(),
                    connection.RemoteEndPoint?.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1, Level = EventLevel.Verbose)]
        private void ConnectionStart(string connectionId,
            string localEndPoint,
            string remoteEndPoint)
        {
            WriteEvent(
                1,
                connectionId,
                localEndPoint,
                remoteEndPoint
            );
        }

        [NonEvent]
        public void ConnectionStop(BaseConnectionContext connection)
        {
            Interlocked.Decrement(ref _currentConnections);
            if (IsEnabled())
            {
                ConnectionStop(connection.ConnectionId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(2, Level = EventLevel.Verbose)]
        private void ConnectionStop(string connectionId)
        {
            WriteEvent(2, connectionId);
        }

        [NonEvent]
        public void RequestStart(HttpProtocol httpProtocol)
        {
            // avoid allocating the trace identifier unless logging is enabled
            if (IsEnabled())
            {
                RequestStart(httpProtocol.ConnectionIdFeature, httpProtocol.TraceIdentifier);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(3, Level = EventLevel.Verbose)]
        private void RequestStart(string connectionId, string requestId)
        {
            WriteEvent(3, connectionId, requestId);
        }

        [NonEvent]
        public void RequestStop(HttpProtocol httpProtocol)
        {
            // avoid allocating the trace identifier unless logging is enabled
            if (IsEnabled())
            {
                RequestStop(httpProtocol.ConnectionIdFeature, httpProtocol.TraceIdentifier);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(4, Level = EventLevel.Verbose)]
        private void RequestStop(string connectionId, string requestId)
        {
            WriteEvent(4, connectionId, requestId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(5, Level = EventLevel.Verbose)]
        public void ConnectionRejected(string connectionId)
        {
            if (IsEnabled())
            {
                WriteEvent(5, connectionId);
            }
        }

        [NonEvent]
        public void ConnectionQueued(BaseConnectionContext connection)
        {
            Interlocked.Increment(ref _connectionQueueLength);
            if (IsEnabled())
            {
                ConnectionQueued(
                    connection.ConnectionId,
                    connection.LocalEndPoint?.ToString(),
                    connection.RemoteEndPoint?.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(6, Level = EventLevel.Verbose)]
        private void ConnectionQueued(string connectionId,
            string localEndPoint,
            string remoteEndPoint)
        {
            WriteEvent(
                6,
                connectionId,
                localEndPoint,
                remoteEndPoint
            );
        }

        [NonEvent]
        public void ConnectionDequeued(BaseConnectionContext connection)
        {
            Interlocked.Decrement(ref _connectionQueueLength);
            if (IsEnabled())
            {
                ConnectionDequeued(
                    connection.ConnectionId,
                    connection.LocalEndPoint?.ToString(),
                    connection.RemoteEndPoint?.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(7, Level = EventLevel.Verbose)]
        private void ConnectionDequeued(string connectionId,
            string localEndPoint,
            string remoteEndPoint)
        {
            WriteEvent(
                7,
                connectionId,
                localEndPoint,
                remoteEndPoint
            );
        }


        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
                // They aren't disabled afterwards...

                _connectionsPerSecondCounter ??= new IncrementingPollingCounter("requests-per-second", this, () => _totalConnections)
                {
                    DisplayName = "Connection Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _totalConnectionsCounter ??= new PollingCounter("total-connections", this, () => _totalConnections)
                {
                    DisplayName = "Total Connections",
                };

                _currentConnectionsCounter ??= new PollingCounter("current-connections", this, () => _currentConnections)
                {
                    DisplayName = "Current Connections"
                };

                _connectionQueueLengthCounter ??= new PollingCounter("connection-queue-length", this, () => _connectionQueueLength)
                {
                    DisplayName = "Connection Queue Length"
                };
            }
        }
    }
}

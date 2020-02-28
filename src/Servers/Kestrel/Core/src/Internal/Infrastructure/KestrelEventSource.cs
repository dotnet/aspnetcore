// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    [EventSource(Name = "Microsoft-AspNetCore-Server-Kestrel")]
    internal sealed class KestrelEventSource : EventSource
    {
        public static readonly KestrelEventSource Log = new KestrelEventSource();

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
    }
}

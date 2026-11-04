// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed class HttpConnectionsEventSource : EventSource
{
    public static readonly HttpConnectionsEventSource Log = new HttpConnectionsEventSource();

    private PollingCounter? _connectionsStartedCounter;
    private PollingCounter? _connectionsStoppedCounter;
    private PollingCounter? _connectionsTimedOutCounter;
    private PollingCounter? _currentConnectionsCounter;
    private EventCounter? _connectionDuration;

    private long _connectionsStarted;
    private long _connectionsStopped;
    private long _connectionsTimedOut;
    private long _currentConnections;

    internal HttpConnectionsEventSource()
        : base("Microsoft.AspNetCore.Http.Connections")
    {
    }

    // Used for testing
    internal HttpConnectionsEventSource(string eventSourceName)
        : base(eventSourceName, EventSourceSettings.EtwManifestEventFormat)
    {
    }

    // This has to go through NonEvent because only Primitive types are allowed
    // in function parameters for Events
    [NonEvent]
    public void ConnectionStop(string connectionId, long startTimestamp, long currentTimestamp)
    {
        Interlocked.Increment(ref _connectionsStopped);
        Interlocked.Decrement(ref _currentConnections);

        if (IsEnabled())
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _connectionDuration!.WriteMetric(duration.TotalMilliseconds);

            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                ConnectionStop(connectionId);
            }
        }
    }

    [Event(eventId: 1, Level = EventLevel.Informational, Message = "Started connection '{0}'.")]
    public void ConnectionStart(string connectionId)
    {
        Interlocked.Increment(ref _connectionsStarted);
        Interlocked.Increment(ref _currentConnections);

        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(1, connectionId);
        }
    }

    [Event(eventId: 2, Level = EventLevel.Informational, Message = "Stopped connection '{0}'.")]
    private void ConnectionStop(string connectionId) => WriteEvent(2, connectionId);

    [Event(eventId: 3, Level = EventLevel.Informational, Message = "Connection '{0}' timed out.")]
    public void ConnectionTimedOut(string connectionId)
    {
        Interlocked.Increment(ref _connectionsTimedOut);

        if (IsEnabled(EventLevel.Informational, EventKeywords.None))
        {
            WriteEvent(3, connectionId);
        }
    }

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
            // They aren't disabled afterwards...

            _connectionsStartedCounter ??= new PollingCounter("connections-started", this, () => Volatile.Read(ref _connectionsStarted))
            {
                DisplayName = "Total Connections Started",
            };
            _connectionsStoppedCounter ??= new PollingCounter("connections-stopped", this, () => Volatile.Read(ref _connectionsStopped))
            {
                DisplayName = "Total Connections Stopped",
            };
            _connectionsTimedOutCounter ??= new PollingCounter("connections-timed-out", this, () => Volatile.Read(ref _connectionsTimedOut))
            {
                DisplayName = "Total Connections Timed Out",
            };
            _currentConnectionsCounter ??= new PollingCounter("current-connections", this, () => Volatile.Read(ref _currentConnections))
            {
                DisplayName = "Current Connections",
            };

            _connectionDuration ??= new EventCounter("connections-duration", this)
            {
                DisplayName = "Average Connection Duration",
                DisplayUnits = "ms",
            };
        }
    }

    // 4, ScanningConnections - removed

    // 5, ScannedConnections - removed
}

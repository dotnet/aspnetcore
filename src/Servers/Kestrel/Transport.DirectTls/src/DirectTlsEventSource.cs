// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

[EventSource(Name = EventSourceName)]
internal sealed class DirectTlsEventSource : EventSource
{
    public const string EventSourceName = "Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls";

    public static readonly DirectTlsEventSource Log = new();

    private PollingCounter? _connectionsOwnedCounter;
    private IncrementingPollingCounter? _acceptsCounter;
    private PollingCounter? _readConnectionsPausedCounter;
    private PollingCounter? _writeConnectionsPausedCounter;
    private IncrementingPollingCounter? _bytesReadCounter;
    private IncrementingPollingCounter? _bytesWrittenCounter;
    private IncrementingPollingCounter? _epollWaitsCounter;
    private IncrementingPollingCounter? _epollWakeupsCounter;
    private IncrementingPollingCounter? _epollTimeoutsCounter;
    private IncrementingPollingCounter? _epollReadyEventsCounter;
    private EventCounter? _epollReadyBatchSizeCounter;

    private long _connectionsOwned;
    private long _accepts;
    private long _readConnectionsPaused;
    private long _writeConnectionsPaused;
    private long _bytesRead;
    private long _bytesWritten;
    private long _epollWaits;
    private long _epollWakeups;
    private long _epollTimeouts;
    private long _epollReadyEvents;

    public DirectTlsEventSource()
    {
    }

    internal DirectTlsEventSource(string eventSourceName)
        : base(eventSourceName)
    {
    }

    [NonEvent]
    public void Accepted(int pumpId)
    {
        if (IsEnabled())
        {
            ConnectionAcceptedCore(pumpId);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void ConnectionAcceptedCore(int pumpId)
    {
        Interlocked.Increment(ref _accepts);
        ConnectionAccepted(pumpId);
    }

    [NonEvent]
    public void ConnectionOwned(int pumpId, int pumpConnectionCount)
    {
        Interlocked.Increment(ref _connectionsOwned);

        if (IsEnabled())
        {
            PumpConnections(pumpId, pumpConnectionCount);
        }
    }

    [NonEvent]
    public void ConnectionReleased(int pumpId, int pumpConnectionCount)
    {
        var count = Interlocked.Decrement(ref _connectionsOwned);
        Debug.Assert(count >= 0);

        if (IsEnabled())
        {
            PumpConnections(pumpId, pumpConnectionCount);
        }
    }

    [NonEvent]
    public void ReadConnectionPaused()
    {
        var count = Interlocked.Increment(ref _readConnectionsPaused);
        Debug.Assert(count > 0);
    }

    [NonEvent]
    public void ReadConnectionResumed()
    {
        var count = Interlocked.Decrement(ref _readConnectionsPaused);
        Debug.Assert(count >= 0);
    }

    [NonEvent]
    public void WriteConnectionPaused()
    {
        var count = Interlocked.Increment(ref _writeConnectionsPaused);
        Debug.Assert(count > 0);
    }

    [NonEvent]
    public void WriteConnectionResumed()
    {
        var count = Interlocked.Decrement(ref _writeConnectionsPaused);
        Debug.Assert(count >= 0);
    }

    [NonEvent]
    public void BytesRead(int count)
    {
        if (IsEnabled())
        {
            BytesReadCore(count);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void BytesReadCore(int count)
    {
        Debug.Assert(count >= 0);
        Interlocked.Add(ref _bytesRead, count);
    }

    [NonEvent]
    public void BytesWritten(int count)
    {
        if (IsEnabled())
        {
            BytesWrittenCore(count);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void BytesWrittenCore(int count)
    {
        Debug.Assert(count >= 0);
        Interlocked.Add(ref _bytesWritten, count);
    }

    [NonEvent]
    public void EpollWaitCompleted(int readyEventCount)
    {
        if (IsEnabled())
        {
            EpollWaitCompletedCore(readyEventCount);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void EpollWaitCompletedCore(int readyEventCount)
    {
        Debug.Assert(readyEventCount >= 0);
        Interlocked.Increment(ref _epollWaits);

        if (readyEventCount == 0)
        {
            Interlocked.Increment(ref _epollTimeouts);
            return;
        }

        Interlocked.Increment(ref _epollWakeups);
        Interlocked.Add(ref _epollReadyEvents, readyEventCount);
        _epollReadyBatchSizeCounter?.WriteMetric(readyEventCount);
    }

    [NonEvent]
    public void RecordEpollConnectionRegistered(int pumpId, int fileDescriptor, string connectionId, uint events)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
        {
            EpollConnectionRegisteredCore(pumpId, fileDescriptor, connectionId, events);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void EpollConnectionRegisteredCore(int pumpId, int fileDescriptor, string connectionId, uint events)
        => EpollConnectionRegistered(pumpId, fileDescriptor, connectionId, events);

    [NonEvent]
    public void RecordEpollInterestChanged(int pumpId, int fileDescriptor, string connectionId, uint previousEvents, uint events)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
        {
            EpollInterestChangedCore(pumpId, fileDescriptor, connectionId, previousEvents, events);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void EpollInterestChangedCore(int pumpId, int fileDescriptor, string connectionId, uint previousEvents, uint events)
        => EpollInterestChanged(pumpId, fileDescriptor, connectionId, previousEvents, events);

    [NonEvent]
    public void RecordEpollReady(int pumpId, int fileDescriptor, string connectionId, uint events)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
        {
            EpollReadyCore(pumpId, fileDescriptor, connectionId, events);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void EpollReadyCore(int pumpId, int fileDescriptor, string connectionId, uint events)
        => EpollReady(pumpId, fileDescriptor, connectionId, events);

    [NonEvent]
    public void RecordEpollConnectionUnregistered(int pumpId, int fileDescriptor, string connectionId, uint events)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
        {
            EpollConnectionUnregisteredCore(pumpId, fileDescriptor, connectionId, events);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [NonEvent]
    private void EpollConnectionUnregisteredCore(int pumpId, int fileDescriptor, string connectionId, uint events)
        => EpollConnectionUnregistered(pumpId, fileDescriptor, connectionId, events);

    [Event(1, Level = EventLevel.Informational)]
    private void ConnectionAccepted(int pumpId)
        => WriteEvent(1, pumpId);

    [Event(2, Level = EventLevel.Informational)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PumpConnections(int pumpId, int connectionCount)
        => WriteEvent(2, pumpId, connectionCount);

    [Event(3, Level = EventLevel.Verbose)]
    private void EpollConnectionRegistered(int pumpId, int fileDescriptor, string connectionId, uint events)
        => WriteEvent(3, pumpId, fileDescriptor, connectionId, events);

    [Event(4, Level = EventLevel.Verbose)]
    private void EpollInterestChanged(int pumpId, int fileDescriptor, string connectionId, uint previousEvents, uint events)
        => WriteEvent(4, pumpId, fileDescriptor, connectionId, previousEvents, events);

    [Event(5, Level = EventLevel.Verbose)]
    private void EpollReady(int pumpId, int fileDescriptor, string connectionId, uint events)
        => WriteEvent(5, pumpId, fileDescriptor, connectionId, events);

    [Event(6, Level = EventLevel.Verbose)]
    private void EpollConnectionUnregistered(int pumpId, int fileDescriptor, string connectionId, uint events)
        => WriteEvent(6, pumpId, fileDescriptor, connectionId, events);

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            _connectionsOwnedCounter ??= new PollingCounter("connections-owned", this, () => Interlocked.Read(ref _connectionsOwned))
            {
                DisplayName = "Connections Owned"
            };
            _acceptsCounter ??= new IncrementingPollingCounter("accepts", this, () => Interlocked.Read(ref _accepts))
            {
                DisplayName = "Connections Accepted",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _readConnectionsPausedCounter ??= new PollingCounter("read-connections-paused", this, () => Interlocked.Read(ref _readConnectionsPaused))
            {
                DisplayName = "Connections Paused by Read Backpressure"
            };
            _writeConnectionsPausedCounter ??= new PollingCounter("write-connections-paused", this, () => Interlocked.Read(ref _writeConnectionsPaused))
            {
                DisplayName = "Connections Paused by Write Backpressure"
            };
            _bytesReadCounter ??= new IncrementingPollingCounter("bytes-read", this, () => Interlocked.Read(ref _bytesRead))
            {
                DisplayName = "Bytes Read",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _bytesWrittenCounter ??= new IncrementingPollingCounter("bytes-written", this, () => Interlocked.Read(ref _bytesWritten))
            {
                DisplayName = "Bytes Written",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _epollWaitsCounter ??= new IncrementingPollingCounter("epoll-waits", this, () => Interlocked.Read(ref _epollWaits))
            {
                DisplayName = "Epoll Waits",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _epollWakeupsCounter ??= new IncrementingPollingCounter("epoll-wakeups", this, () => Interlocked.Read(ref _epollWakeups))
            {
                DisplayName = "Epoll Wakeups",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _epollTimeoutsCounter ??= new IncrementingPollingCounter("epoll-timeouts", this, () => Interlocked.Read(ref _epollTimeouts))
            {
                DisplayName = "Epoll Timeouts",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _epollReadyEventsCounter ??= new IncrementingPollingCounter("epoll-ready-events", this, () => Interlocked.Read(ref _epollReadyEvents))
            {
                DisplayName = "Epoll Ready Events",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
            _epollReadyBatchSizeCounter ??= new EventCounter("epoll-ready-batch-size", this)
            {
                DisplayName = "Epoll Ready Batch Size"
            };
        }
    }
}

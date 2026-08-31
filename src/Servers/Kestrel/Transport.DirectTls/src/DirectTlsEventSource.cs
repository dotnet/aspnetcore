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
    private PollingCounter? _connectionsPausedCounter;
    private IncrementingPollingCounter? _bytesReadCounter;
    private IncrementingPollingCounter? _bytesWrittenCounter;

    private long _connectionsOwned;
    private long _accepts;
    private long _connectionsPaused;
    private long _bytesRead;
    private long _bytesWritten;

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
    public void ConnectionPaused()
    {
        var count = Interlocked.Increment(ref _connectionsPaused);
        Debug.Assert(count > 0);
    }

    [NonEvent]
    public void ConnectionResumed()
    {
        var count = Interlocked.Decrement(ref _connectionsPaused);
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

    [Event(1, Level = EventLevel.Informational)]
    private void ConnectionAccepted(int pumpId)
        => WriteEvent(1, pumpId);

    [Event(2, Level = EventLevel.Informational)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PumpConnections(int pumpId, int connectionCount)
        => WriteEvent(2, pumpId, connectionCount);

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
            _connectionsPausedCounter ??= new PollingCounter("connections-paused", this, () => Interlocked.Read(ref _connectionsPaused))
            {
                DisplayName = "Connections Paused by Backpressure"
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
        }
    }
}

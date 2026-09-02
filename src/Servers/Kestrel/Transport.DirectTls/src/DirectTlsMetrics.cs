// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

internal sealed class DirectTlsMetrics
{
    public const string ReadBackpressureConnectionsInstrumentName = "kestrel.direct_tls.read_backpressure_connections";
    public const string WriteBackpressureConnectionsInstrumentName = "kestrel.direct_tls.write_backpressure_connections";

    public static readonly DirectTlsMetrics Disabled = new();

    private readonly UpDownCounter<long>? _readBackpressureConnections;
    private readonly UpDownCounter<long>? _writeBackpressureConnections;
    private readonly DirectTlsEventSource _eventSource = DirectTlsEventSource.Log;

    private DirectTlsMetrics()
    {
    }

    public DirectTlsMetrics(IMeterFactory meterFactory)
        : this(meterFactory.Create(KestrelMetrics.MeterName))
    {
    }

    internal DirectTlsMetrics(Meter meter)
    {
        _readBackpressureConnections = meter.CreateUpDownCounter<long>(
            ReadBackpressureConnectionsInstrumentName,
            unit: "{connection}",
            description: "Number of DirectTls connections paused by input-pipe backpressure.");
        _writeBackpressureConnections = meter.CreateUpDownCounter<long>(
            WriteBackpressureConnectionsInstrumentName,
            unit: "{connection}",
            description: "Number of DirectTls connections whose TLS writes are waiting for socket writability.");
    }

    public bool ReadConnectionPaused(BaseConnectionContext? connection)
    {
        _eventSource.ReadConnectionPaused();

        if (_readBackpressureConnections?.Enabled == true)
        {
            ConnectionPausedCore(_readBackpressureConnections, connection);
            return true;
        }

        return false;
    }

    public void ReadConnectionResumed(bool counterEnabled, BaseConnectionContext? connection)
    {
        _eventSource.ReadConnectionResumed();

        if (counterEnabled)
        {
            ConnectionResumedCore(_readBackpressureConnections!, connection);
        }
    }

    public bool WriteConnectionPaused(BaseConnectionContext? connection)
    {
        _eventSource.WriteConnectionPaused();

        if (_writeBackpressureConnections?.Enabled == true)
        {
            ConnectionPausedCore(_writeBackpressureConnections, connection);
            return true;
        }

        return false;
    }

    public void WriteConnectionResumed(bool counterEnabled, BaseConnectionContext? connection)
    {
        _eventSource.WriteConnectionResumed();

        if (counterEnabled)
        {
            ConnectionResumedCore(_writeBackpressureConnections!, connection);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConnectionPausedCore(UpDownCounter<long> counter, BaseConnectionContext? connection)
    {
        var tags = new TagList();
        if (connection is not null)
        {
            ConnectionEndpointTags.AddConnectionEndpointTags(ref tags, connection);
        }

        counter.Add(1, tags);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConnectionResumedCore(UpDownCounter<long> counter, BaseConnectionContext? connection)
    {
        var tags = new TagList();
        if (connection is not null)
        {
            ConnectionEndpointTags.AddConnectionEndpointTags(ref tags, connection);
        }

        counter.Add(-1, tags);
    }

    public void BytesRead(int count)
        => _eventSource.BytesRead(count);

    public void BytesWritten(int count)
        => _eventSource.BytesWritten(count);
}

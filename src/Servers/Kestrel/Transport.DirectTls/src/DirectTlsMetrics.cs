// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

internal sealed class DirectTlsMetrics
{
    public const string PausedConnectionsInstrumentName = "kestrel.direct_tls.connections_paused";

    public static readonly DirectTlsMetrics Disabled = new();

    private readonly UpDownCounter<long>? _pausedConnections;
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
        _pausedConnections = meter.CreateUpDownCounter<long>(
            PausedConnectionsInstrumentName,
            unit: "{connection}",
            description: "Number of DirectTls connections that are paused by input-pipe backpressure.");
    }

    public bool ConnectionPaused()
    {
        _eventSource.ConnectionPaused();

        if (_pausedConnections?.Enabled == true)
        {
            ConnectionPausedCore();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionPausedCore()
        => _pausedConnections!.Add(1);

    public void ConnectionResumed(bool pausedConnectionsCounterEnabled)
    {
        _eventSource.ConnectionResumed();

        if (pausedConnectionsCounterEnabled)
        {
            ConnectionResumedCore();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ConnectionResumedCore()
        => _pausedConnections!.Add(-1);

    public void BytesRead(int count)
        => _eventSource.BytesRead(count);

    public void BytesWritten(int count)
        => _eventSource.BytesWritten(count);
}

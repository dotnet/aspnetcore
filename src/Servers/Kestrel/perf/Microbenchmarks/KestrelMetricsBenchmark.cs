// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class KestrelMetricsBenchmark
{
    private TestMeterFactory _meterFactory;
    private MeterListener _listener;
    private KestrelMetrics _metrics;
    private ConnectionMetricsContext[] _metricsContexts;
    private int _next;

    // The number of distinct local ports that streams alternate between. With 2, the local port
    // changes on every stream, so any single-entry cache of the boxed server.port value misses every time.
    [Params(1, 2)]
    public int Ports { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _meterFactory = new TestMeterFactory();
        _metrics = new KestrelMetrics(_meterFactory);

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Scope == _meterFactory && instrument.Name == "kestrel.queued_requests")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<long>(static (_, _, _, _) => { });
        _listener.Start();

        _metricsContexts = new ConnectionMetricsContext[Ports];
        for (var i = 0; i < Ports; i++)
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application)
            {
                LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 5000 + i),
                RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 50000),
            };

            _metricsContexts[i] = _metrics.CreateContext(connection);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _listener.Dispose();
        _meterFactory.Dispose();
    }

    // Mirrors the kestrel.queued_requests measurements made for each HTTP/2 stream.
    [Benchmark]
    public void QueuedRequest()
    {
        var metricsContext = _metricsContexts[_next++ % _metricsContexts.Length];
        _metrics.RequestQueuedStart(metricsContext, KestrelMetrics.Http2);
        _metrics.RequestQueuedStop(metricsContext, KestrelMetrics.Http2);
    }
}

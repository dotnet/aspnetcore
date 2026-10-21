// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubConnectionContextCreationBenchmark
{
    private TestConnectionContext _connection;
    private HubConnectionContextOptions _options;
    private CancellationTokenSource _invocationCancellation;

    [Params(false, true)]
    public bool TrackCancellation;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new TestConnectionContext { ConnectionId = "benchmark" };
        _options = new HubConnectionContextOptions
        {
            KeepAliveInterval = Timeout.InfiniteTimeSpan,
        };
        _invocationCancellation = new CancellationTokenSource();
    }

    [Benchmark]
    public HubConnectionContext CreateAndCleanup()
    {
        var context = new HubConnectionContext(_connection, _options, NullLoggerFactory.Instance);
        if (TrackCancellation)
        {
            context.ActiveRequestCancellationSources.TryAdd("invocation", _invocationCancellation);
        }

        context.Cleanup();
        return context;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _invocationCancellation.Dispose();
    }
}

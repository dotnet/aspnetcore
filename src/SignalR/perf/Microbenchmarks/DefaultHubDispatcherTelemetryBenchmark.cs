// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class DefaultHubDispatcherTelemetryBenchmark
{
    private DefaultHubDispatcher<DefaultHubDispatcherBenchmark.TestHub> _dispatcher;
    private HubConnectionContext[] _connectionContexts;
    private int _next;
    private ActivityListener _listener;
    private Activity _connectionActivity;

    [Params(false, true)]
    public bool Tracing { get; set; }

    // The number of distinct local ports that invocations alternate between. With 2, the local port
    // changes on every invocation, so any single-entry cache of the boxed server.port value misses every time.
    [Params(1, 2)]
    public int Ports { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        if (Tracing)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SignalRServerActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSignalRCore();

        var provider = serviceCollection.BuildServiceProvider();

        var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();

        var hubLifetimeManager = new DefaultHubLifetimeManager<DefaultHubDispatcherBenchmark.TestHub>(NullLogger<DefaultHubLifetimeManager<DefaultHubDispatcherBenchmark.TestHub>>.Instance);
        _dispatcher = new DefaultHubDispatcher<DefaultHubDispatcherBenchmark.TestHub>(
            serviceScopeFactory,
            new HubContext<DefaultHubDispatcherBenchmark.TestHub>(hubLifetimeManager),
            enableDetailedErrors: false,
            disableImplicitFromServiceParameters: true,
            new Logger<DefaultHubDispatcher<DefaultHubDispatcherBenchmark.TestHub>>(NullLoggerFactory.Instance),
            hubFilters: null,
            hubLifetimeManager);

        // Invocation activities are linked to the activity that was current when the connection was established.
        _connectionActivity = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        _connectionActivity.SetIdFormat(ActivityIdFormat.W3C);
        _connectionActivity.Start();
        _connectionActivity.Stop();

        _connectionContexts = new HubConnectionContext[Ports];
        for (var i = 0; i < Ports; i++)
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Application, pair.Transport)
            {
                LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 5000 + i),
                RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 50000),
            };

            var contextOptions = new HubConnectionContextOptions()
            {
                KeepAliveInterval = TimeSpan.Zero,
                StreamBufferCapacity = 10,
            };

            _connectionContexts[i] = new DefaultHubDispatcherBenchmark.NoErrorHubConnectionContext(connection, contextOptions, NullLoggerFactory.Instance)
            {
                OriginalActivity = _connectionActivity,
                Protocol = new DefaultHubDispatcherBenchmark.FakeHubProtocol()
            };
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _listener?.Dispose();
    }

    [Benchmark]
    public Task Invocation()
    {
        var connectionContext = _connectionContexts[_next++ % _connectionContexts.Length];
        return _dispatcher.DispatchMessageAsync(connectionContext, new InvocationMessage("123", "Invocation", Array.Empty<object>()));
    }
}

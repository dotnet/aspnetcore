// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class SendConnectionsBenchmark
{
    private DefaultHubLifetimeManager<Hub> _hubLifetimeManager;
    private string[] _connectionIds;
    private readonly object[] _arguments = ["Hello world!"];

    [Params(10, 1000)]
    public int Connections;

    [Params(1, 2, 10)]
    public int TargetConnections;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _hubLifetimeManager = new DefaultHubLifetimeManager<Hub>(NullLogger<DefaultHubLifetimeManager<Hub>>.Instance);
        _connectionIds = new string[TargetConnections];
        var protocol = new JsonHubProtocol();
        var options = new HubConnectionContextOptions
        {
            KeepAliveInterval = Timeout.InfiniteTimeSpan,
        };

        for (var i = 0; i < Connections; i++)
        {
            var connection = new TestConnectionContext
            {
                ConnectionId = Guid.NewGuid().ToString(),
                Transport = new TestDuplexPipe(),
            };
            var hubConnection = new HubConnectionContext(connection, options, NullLoggerFactory.Instance)
            {
                Protocol = protocol,
            };
            _hubLifetimeManager.OnConnectedAsync(hubConnection).GetAwaiter().GetResult();

            if (i < _connectionIds.Length)
            {
                _connectionIds[i] = connection.ConnectionId;
            }
        }
    }

    [Benchmark]
    public Task SendAsync()
    {
        return _hubLifetimeManager.SendConnectionsAsync(_connectionIds, "Method", _arguments);
    }
}

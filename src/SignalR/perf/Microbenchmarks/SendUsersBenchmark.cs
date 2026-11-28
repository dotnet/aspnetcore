// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class SendUsersBenchmark
{
    private DefaultHubLifetimeManager<Hub> _hubLifetimeManager;
    private string[] _userIds;
    private readonly object[] _arguments = ["Hello world!"];

    [Params(10, 1000, 10000)]
    public int Connections;

    // One connection per user.
    [Params(1, 10)]
    public int TargetUsers;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _hubLifetimeManager = new DefaultHubLifetimeManager<Hub>(NullLogger<DefaultHubLifetimeManager<Hub>>.Instance);
        _userIds = new string[TargetUsers];
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
                UserIdentifier = $"user{i}",
            };
            _hubLifetimeManager.OnConnectedAsync(hubConnection).GetAwaiter().GetResult();

            if (i < _userIds.Length)
            {
                _userIds[i] = hubConnection.UserIdentifier;
            }
        }
    }

    [Benchmark]
    public Task SendUserAsync()
    {
        return _hubLifetimeManager.SendUserAsync(_userIds[0], "Method", _arguments);
    }

    [Benchmark]
    public Task SendUsersAsync()
    {
        return _hubLifetimeManager.SendUsersAsync(_userIds, "Method", _arguments);
    }
}

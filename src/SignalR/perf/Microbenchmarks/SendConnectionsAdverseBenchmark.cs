// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class SendConnectionsAdverseBenchmark
{
    private readonly object[] _arguments = ["value"];
    private DefaultHubLifetimeManager<Hub> _manager;
    private HubConnectionStore _connections;
    private string[] _targets;

    [Params("Empty", "DuplicateHeavy", "Missing", "Dense")]
    public string Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _manager = new DefaultHubLifetimeManager<Hub>(NullLogger<DefaultHubLifetimeManager<Hub>>.Instance);
        _connections = new HubConnectionStore();
        var connectionCount = Scenario switch
        {
            "Empty" => 0,
            "Dense" => 1000,
            _ => 10,
        };
        var ids = new string[connectionCount];
        for (var i = 0; i < connectionCount; i++)
        {
            ids[i] = $"connection-{i}";
            var connection = new CountingConnection(new DefaultConnectionContext(ids[i]));
            _connections.Add(connection);
            _manager.OnConnectedAsync(connection).GetAwaiter().GetResult();
        }

        _targets = new string[Scenario == "Dense" ? connectionCount : 100_000];
        for (var i = 0; i < _targets.Length; i++)
        {
            _targets[i] = Scenario is "Empty" or "Missing" ? "missing" : ids[i % ids.Length];
        }

        OriginalScan().GetAwaiter().GetResult();
        IndexedSend().GetAwaiter().GetResult();
        foreach (CountingConnection connection in _connections)
        {
            var expected = Scenario == "Missing" ? 0 : 2;
            if (connection.Writes != expected)
            {
                throw new InvalidOperationException("Recipients were missed or received duplicate messages.");
            }
        }
    }

    // Preserve the original implementation as a control for input-dependent complexity.
    [Benchmark(Baseline = true)]
    public Task OriginalScan()
    {
        return SendToAllConnections("Method", _arguments,
            (connection, state) => ((IReadOnlyList<string>)state).Contains(connection.ConnectionId), _targets);
    }

    private Task SendToAllConnections(string methodName, object[] arguments, Func<HubConnectionContext, object, bool> include, object state)
    {
        List<Task> tasks = null;
        SerializedHubMessage message = null;
        foreach (var connection in _connections)
        {
            if (!include(connection, state))
            {
                continue;
            }

            message ??= new SerializedHubMessage(new InvocationMessage(methodName, arguments));
            var task = connection.WriteAsync(message);
            if (!task.IsCompletedSuccessfully)
            {
                tasks ??= new List<Task>();
                tasks.Add(task.AsTask());
            }
            else
            {
                task.GetAwaiter().GetResult();
            }
        }

        return tasks is null ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    [Benchmark]
    public Task IndexedSend() => _manager.SendConnectionsAsync(_targets, "Method", _arguments);

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (CountingConnection connection in _connections)
        {
            connection.Cleanup();
            connection.TransportContext.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private sealed class CountingConnection(DefaultConnectionContext connection)
        : HubConnectionContext(connection, new HubConnectionContextOptions(), NullLoggerFactory.Instance)
    {
        public DefaultConnectionContext TransportContext => connection;
        public int Writes { get; private set; }

        public override ValueTask WriteAsync(SerializedHubMessage message, CancellationToken cancellationToken = default)
        {
            Writes++;
            return ValueTask.CompletedTask;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Tests;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubGroupListConcurrencyBenchmark
{
    private const int ChurnOperations = 1024;
    private HubGroupList _groups;
    private HubConnectionContext[] _connections;
    private string[] _groupNames;
    private ParallelOptions _parallelOptions;
    private Action<int> _churnGroup;

    [Params(1, 8)]
    public int Workers;

    [Params(1, 1024)]
    public int ConnectionsPerGroup;

    [Params(false, true)]
    public bool SharedGroup;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _groups = new HubGroupList();
        _connections = new HubConnectionContext[Workers];
        _groupNames = new string[SharedGroup ? 1 : Workers];
        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Workers };
        _churnGroup = ChurnGroup;

        for (var i = 0; i < Workers; i++)
        {
            _connections[i] = HubConnectionContextUtils.Create(new TestConnectionContext { ConnectionId = "churn-" + i });
        }

        for (var i = 0; i < _groupNames.Length; i++)
        {
            _groupNames[i] = "group-" + i;
        }

        for (var i = 0; i < ConnectionsPerGroup; i++)
        {
            var connection = HubConnectionContextUtils.Create(new TestConnectionContext { ConnectionId = "member-" + i });
            foreach (var groupName in _groupNames)
            {
                _groups.Add(connection, groupName);
            }
        }
    }

    [Benchmark(OperationsPerInvoke = ChurnOperations)]
    public void AddAndRemove()
    {
        // One operation is an add/remove pair; batching amortizes Parallel.For scheduling.
        Parallel.For(0, Workers, _parallelOptions, _churnGroup);
    }

    private void ChurnGroup(int worker)
    {
        var connection = _connections[worker];
        var groupName = _groupNames[SharedGroup ? 0 : worker];

        for (var i = 0; i < ChurnOperations / Workers; i++)
        {
            _groups.Add(connection, groupName);
            _groups.Remove(connection.ConnectionId, groupName);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_groups.Count != _groupNames.Length)
        {
            throw new InvalidOperationException("The number of groups changed during the benchmark.");
        }

        foreach (var groupName in _groupNames)
        {
            if (_groups[groupName]?.Count != ConnectionsPerGroup)
            {
                throw new InvalidOperationException("Group membership changed during the benchmark.");
            }
        }
    }
}

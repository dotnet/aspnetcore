// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Tests;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubGroupListBenchmark
{
    private const string GroupName = "group";
    private HubGroupList _groups;
    private HubConnectionContext _connection;

    [Params(0, 1, 32, 1024)]
    public int ConnectionsPerGroup;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _groups = new HubGroupList();
        _connection = HubConnectionContextUtils.Create(new TestConnectionContext { ConnectionId = "churn" });

        for (var i = 0; i < ConnectionsPerGroup; i++)
        {
            var connection = HubConnectionContextUtils.Create(new TestConnectionContext { ConnectionId = "member-" + i });
            _groups.Add(connection, GroupName);
        }
    }

    [Benchmark]
    public void AddAndRemove()
    {
        // With no permanent members, each pair also creates and removes the group.
        _groups.Add(_connection, GroupName);
        _groups.Remove(_connection.ConnectionId, GroupName);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        var group = _groups[GroupName];
        if (ConnectionsPerGroup == 0 ? group is not null : group?.Count != ConnectionsPerGroup)
        {
            throw new InvalidOperationException("Group membership changed during the benchmark.");
        }
    }
}

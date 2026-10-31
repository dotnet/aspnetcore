// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class HubGroupListTests
{
    [Fact]
    public void AddCreatesGroupAndAddsToExistingGroup()
    {
        var groups = new HubGroupList();
        var first = CreateConnection("first");
        var second = CreateConnection("second");

        groups.Add(first, "group");
        groups.Add(second, "group");
        groups.Add(second, "group");

        var group = groups["group"];
        Assert.NotNull(group);
        Assert.Equal(2, group.Count);
        Assert.Same(first, group["first"]);
        Assert.Same(second, group["second"]);
        Assert.Single(groups);
    }

    [Fact]
    public void RemovingLastConnectionRemovesGroupAndGroupCanBeRecreated()
    {
        var groups = new HubGroupList();
        var first = CreateConnection("first");
        var second = CreateConnection("second");

        groups.Add(first, "group");
        groups.Add(second, "group");

        groups.Remove("first", "group");
        Assert.Same(second, Assert.Single(groups["group"]!).Value);

        groups.Remove("second", "group");
        Assert.Null(groups["group"]);
        Assert.Empty(groups);

        groups.Add(first, "group");
        var group = groups["group"];
        Assert.NotNull(group);
        Assert.Same(first, Assert.Single(group).Value);
    }

    [Fact]
    public async Task ConcurrentAddAndRemoveDoesNotLoseConnections()
    {
        const int Workers = 4;
        const int Iterations = 10_000;

        var groups = new HubGroupList();
        var stable = CreateConnection("stable");
        var churn = Enumerable.Range(0, Workers).Select(i => CreateConnection("churn-" + i)).ToArray();
        using var start = new Barrier(Workers + 1);

        var workers = churn.Select(connection => Task.Run(() =>
        {
            start.SignalAndWait();
            for (var i = 0; i < Iterations; i++)
            {
                groups.Add(connection, "group");
                groups.Remove(connection.ConnectionId, "group");
            }
        })).ToArray();

        var stableTask = Task.Run(() =>
        {
            start.SignalAndWait();
            for (var i = 0; i < Iterations; i++)
            {
                groups.Add(stable, "group");
            }
        });

        await Task.WhenAll(workers).DefaultTimeout();
        await stableTask.DefaultTimeout();

        var group = groups["group"];
        Assert.NotNull(group);
        Assert.Same(stable, Assert.Single(group).Value);
    }

    private static HubConnectionContext CreateConnection(string connectionId)
        => HubConnectionContextUtils.Create(new TestConnectionContext { ConnectionId = connectionId });
}

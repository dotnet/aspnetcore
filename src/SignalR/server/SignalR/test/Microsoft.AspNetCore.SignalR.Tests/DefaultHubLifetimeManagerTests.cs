// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Specification.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class DefaultHubLifetimeManagerTests : HubLifetimeManagerTestsBase<Hub>
{
    public override HubLifetimeManager<Hub> CreateNewHubLifetimeManager()
    {
        return new DefaultHubLifetimeManager<Hub>(new Logger<DefaultHubLifetimeManager<Hub>>(NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task SendAllAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendAllAsync("Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());
            var message = Assert.IsType<InvocationMessage>(client1.TryRead());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task SendAllExceptAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendAllExceptAsync("Hello", new object[] { "World" }, new List<string> { connection1.ConnectionId }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
            Assert.Null(client1.TryRead());
        }
    }

    [Fact]
    public async Task SendConnectionAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendConnectionAsync(connection1.ConnectionId, "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task SendConnectionsAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendConnectionsAsync(new List<string> { connection1.ConnectionId }, "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SendConnectionsAsyncWritesToSpecifiedConnections(int targetCount)
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        using var client3 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);
        var connection3 = HubConnectionContextUtils.Create(client3.Connection);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();
        await manager.OnConnectedAsync(connection3).DefaultTimeout();

        string[] connectionIds = targetCount switch
        {
            0 => [],
            1 => [connection1.ConnectionId],
            _ => [connection1.ConnectionId, connection2.ConnectionId],
        };

        var sendTask = manager.SendConnectionsAsync(connectionIds, "Hello", ["World"]);
        Assert.True(sendTask.IsCompletedSuccessfully);
        await sendTask.DefaultTimeout();

        TestClient[] clients = [client1, client2, client3];
        for (var i = 0; i < clients.Length; i++)
        {
            if (i < targetCount)
            {
                var message = Assert.IsType<InvocationMessage>(clients[i].TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Equal("World", Assert.Single(message.Arguments));
            }

            Assert.Null(clients[i].TryRead());
        }
    }

    [Theory]
    [InlineData(17)]
    [InlineData(100_000)]
    public async Task SendConnectionsAsyncDoesNotInspectLargeTargetListsWhenThereAreNoConnections(int targetCount)
    {
        var targets = new Mock<IReadOnlyList<string>>(MockBehavior.Strict);
        targets.SetupGet(list => list.Count).Returns(targetCount);
        var manager = CreateNewHubLifetimeManager();

        await manager.SendConnectionsAsync(targets.Object, "Hello", ["World"]).DefaultTimeout();

        targets.VerifyGet(list => list[It.IsAny<int>()], Times.Never);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(16, false)]
    [InlineData(16, true)]
    [InlineData(17, false)]
    [InlineData(17, true)]
    [InlineData(100_000, false)]
    [InlineData(100_000, true)]
    public async Task SendConnectionsAsyncIgnoresDuplicateAndMissingConnectionIds(int targetCount, bool useList)
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        var connectionIds = new string[targetCount];
        for (var i = 0; i < connectionIds.Length; i++)
        {
            connectionIds[i] = (i % 3) switch
            {
                0 => "missing",
                1 => connection1.ConnectionId,
                _ => connection2.ConnectionId,
            };
        }

        IReadOnlyList<string> targets = useList ? new List<string>(connectionIds) : connectionIds;
        await manager.SendConnectionsAsync(targets, "Hello", ["World"]).DefaultTimeout();

        foreach (var client in new[] { client1, client2 })
        {
            var message = Assert.IsType<InvocationMessage>(client.TryRead());
            Assert.Equal("Hello", message.Target);
            Assert.Equal("World", Assert.Single(message.Arguments));
            Assert.Null(client.TryRead());
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(17)]
    public async Task SendConnectionsAsyncUsesCaseSensitiveConnectionIds(int targetCount)
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        client1.Connection.ConnectionId = "connection";
        client2.Connection.ConnectionId = "CONNECTION";
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        var connectionIds = new string[targetCount];
        for (var i = 0; i < connectionIds.Length; i++)
        {
            connectionIds[i] = i % 2 == 0 ? connection1.ConnectionId : connection2.ConnectionId;
        }

        await manager.SendConnectionsAsync(connectionIds, "Hello", ["World"]).DefaultTimeout();

        Assert.IsType<InvocationMessage>(client1.TryRead());
        Assert.IsType<InvocationMessage>(client2.TryRead());
        Assert.Null(client1.TryRead());
        Assert.Null(client2.TryRead());
    }

    [Fact]
    public async Task SendConnectionsAsyncSerializesOnceForConnectionsWithTheSameProtocol()
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var jsonProtocol = new JsonHubProtocol();
        var protocol = new Mock<IHubProtocol>(MockBehavior.Strict);
        protocol.Setup(p => p.Name).Returns(jsonProtocol.Name);
        protocol.Setup(p => p.GetMessageBytes(It.IsAny<HubMessage>()))
            .Returns((HubMessage message) => jsonProtocol.GetMessageBytes(message));

        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection, protocol.Object);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection, protocol.Object);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        await manager.SendConnectionsAsync(
            [connection1.ConnectionId, connection2.ConnectionId], "Hello", ["World"]).DefaultTimeout();

        protocol.Verify(p => p.GetMessageBytes(It.IsAny<HubMessage>()), Times.Once);
        Assert.IsType<InvocationMessage>(client1.TryRead());
        Assert.IsType<InvocationMessage>(client2.TryRead());
    }

    [Fact]
    public async Task SendConnectionsAsyncWaitsForAllWrites()
    {
        using var client1 = new TestClient(pauseWriterThreshold: 2);
        using var client2 = new TestClient(pauseWriterThreshold: 2);
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        var sendTask = manager.SendConnectionsAsync(
            [connection1.ConnectionId, connection2.ConnectionId], "Hello", ["World"]);
        Assert.False(sendTask.IsCompleted);

        Assert.IsType<InvocationMessage>(await client2.ReadAsync().DefaultTimeout());
        Assert.False(sendTask.IsCompleted);
        Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
        await sendTask.DefaultTimeout();
    }

    [Fact]
    public async Task SendConnectionsAsyncWritesToOtherConnectionsWhenAWriteIsCanceled()
    {
        using var client1 = new TestClient(pauseWriterThreshold: 2);
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);
        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();
        using var cts = new CancellationTokenSource();

        var sendTask = manager.SendConnectionsAsync(
            [connection1.ConnectionId, connection2.ConnectionId], "Hello", ["World"], cts.Token);
        Assert.False(sendTask.IsCompleted);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask.DefaultTimeout());
        var message = Assert.IsType<InvocationMessage>(client2.TryRead());
        Assert.Equal("Hello", message.Target);
        Assert.Equal("World", Assert.Single(message.Arguments));
        Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
        Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
    }

    [Fact]
    public async Task SendGroupAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.AddToGroupAsync(connection1.ConnectionId, "group").DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendGroupAsync("group", "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task SendGroupExceptAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();
            await manager.AddToGroupAsync(connection1.ConnectionId, "group").DefaultTimeout();
            await manager.AddToGroupAsync(connection2.ConnectionId, "group").DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendGroupExceptAsync("group", "Hello", new object[] { "World" }, new List<string> { connection1.ConnectionId }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
            Assert.Null(client1.TryRead());
        }
    }

    [Fact]
    public async Task SendGroupsAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.AddToGroupAsync(connection1.ConnectionId, "group").DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendGroupsAsync(new List<string> { "group" }, "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task SendUserAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "user");
            var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "user");
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendUserAsync("user", "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());
            var message = Assert.IsType<InvocationMessage>(client1.TryRead());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task SendUsersAsyncWillCancelWithToken()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient(pauseWriterThreshold: 2))
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "user1");
            var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "user2");
            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();
            var cts = new CancellationTokenSource();
            var sendTask = manager.SendUsersAsync(new List<string> { "user1", "user2" }, "Hello", new object[] { "World" }, cts.Token).DefaultTimeout();
            Assert.False(sendTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask.DefaultTimeout());
            var message = Assert.IsType<InvocationMessage>(client1.TryRead());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            Assert.False(connection2.ConnectionAborted.IsCancellationRequested);
        }
    }

}

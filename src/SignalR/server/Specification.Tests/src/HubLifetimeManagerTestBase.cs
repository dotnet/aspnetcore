// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks.Extensions;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Specification.Tests;

/// <summary>
/// Base test class for lifetime manager implementations. Nothing specific to scale-out for these tests.
/// </summary>
/// <typeparam name="THub">The type of the <see cref="Hub"/>.</typeparam>
public abstract class HubLifetimeManagerTestsBase<THub> where THub : Hub
{
    /// <summary>
    /// This API is obsolete and will be removed in a future version. Use CreateNewHubLifetimeManager in tests instead.
    /// </summary>
    [Obsolete("This API is obsolete and will be removed in a future version. Use CreateNewHubLifetimeManager in tests instead.")]
    public HubLifetimeManager<THub> Manager { get; set; }

    /// <summary>
    /// Method to create an implementation of <see cref="HubLifetimeManager{THub}"/> for use in tests.
    /// </summary>
    /// <returns>The implementation of <see cref="HubLifetimeManager{THub}"/> to test against.</returns>
    public abstract HubLifetimeManager<THub> CreateNewHubLifetimeManager();

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task SendAllAsyncWritesToAllConnectionsOutput()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();

            await manager.SendAllAsync("Hello", new object[] { "World" }).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            message = Assert.IsType<InvocationMessage>(await client2.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task SendAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();

            await manager.OnDisconnectedAsync(connection2).DefaultTimeout();

            await manager.SendAllAsync("Hello", new object[] { "World" }).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.Null(client2.TryRead());
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task SendGroupAsyncWritesToAllConnectionsInGroupOutput()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();

            await manager.AddToGroupAsync(connection1.ConnectionId, "group").DefaultTimeout();

            await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.Null(client2.TryRead());
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task SendGroupExceptAsyncDoesNotWriteToExcludedConnections()
    {
        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var manager = CreateNewHubLifetimeManager();
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();

            await manager.AddToGroupAsync(connection1.ConnectionId, "group1").DefaultTimeout();
            await manager.AddToGroupAsync(connection2.ConnectionId, "group1").DefaultTimeout();

            await manager.SendGroupExceptAsync("group1", "Hello", new object[] { "World" }, new[] { connection2.ConnectionId }).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);

            Assert.Null(client2.TryRead());
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task SendConnectionAsyncWritesToConnectionOutput()
    {
        using (var client = new TestClient())
        {
            var manager = CreateNewHubLifetimeManager();
            var connection = HubConnectionContextUtils.Create(client.Connection);

            await manager.OnConnectedAsync(connection).DefaultTimeout();

            await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task CanProcessClientReturnResult()
    {
        var manager = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();

            var resultTask = manager.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);
            var invocation = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.NotNull(invocation.InvocationId);
            Assert.Equal("test", invocation.Arguments[0]);

            await manager.SetConnectionResultAsync(connection1.ConnectionId, CompletionMessage.WithResult(invocation.InvocationId, 10)).DefaultTimeout();

            var res = await resultTask.DefaultTimeout();
            Assert.Equal(10L, res);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task CanProcessClientReturnErrorResult()
    {
        var manager = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();

            var resultTask = manager.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);
            var invocation = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.NotNull(invocation.InvocationId);
            Assert.Equal("test", invocation.Arguments[0]);

            await manager.SetConnectionResultAsync(connection1.ConnectionId, CompletionMessage.WithError(invocation.InvocationId, "Error from client")).DefaultTimeout();

            var ex = await Assert.ThrowsAsync<HubException>(() => resultTask).DefaultTimeout();
            Assert.Equal("Error from client", ex.Message);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task ExceptionWhenIncorrectClientCompletesClientResult()
    {
        var manager = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager.OnConnectedAsync(connection1).DefaultTimeout();
            await manager.OnConnectedAsync(connection2).DefaultTimeout();

            var resultTask = manager.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);
            var invocation = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.NotNull(invocation.InvocationId);
            Assert.Equal("test", invocation.Arguments[0]);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                manager.SetConnectionResultAsync(connection2.ConnectionId, CompletionMessage.WithError(invocation.InvocationId, "Error from client"))).DefaultTimeout();

            Assert.Equal($"Connection ID '{connection2.ConnectionId}' is not valid for invocation ID '{invocation.InvocationId}'.", ex.Message);

            // Internal state for invocation isn't affected by wrong client, check that we can still complete the invocation
            await manager.SetConnectionResultAsync(connection1.ConnectionId, CompletionMessage.WithResult(invocation.InvocationId, 10)).DefaultTimeout();

            var res = await resultTask.DefaultTimeout();
            Assert.Equal(10L, res);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task ConnectionIDNotPresentWhenInvokingClientResult()
    {
        var manager1 = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);

            await manager1.OnConnectedAsync(connection1).DefaultTimeout();

            // No client with this ID
            await Assert.ThrowsAsync<IOException>(() => manager1.InvokeConnectionAsync<int>("none", "Result", new object[] { "test" }, cancellationToken: default)).DefaultTimeout();
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task InvokesForMultipleClientsDoNotCollide()
    {
        var manager1 = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);
            var connection2 = HubConnectionContextUtils.Create(client2.Connection);

            await manager1.OnConnectedAsync(connection1).DefaultTimeout();
            await manager1.OnConnectedAsync(connection2).DefaultTimeout();

            var invoke1 = manager1.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);
            var invoke2 = manager1.InvokeConnectionAsync<int>(connection2.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);

            var invocation1 = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            var invocation2 = Assert.IsType<InvocationMessage>(await client2.ReadAsync().DefaultTimeout());
            await manager1.SetConnectionResultAsync(connection2.ConnectionId, CompletionMessage.WithError(invocation2.InvocationId, "error"));

            await Assert.ThrowsAnyAsync<Exception>(() => invoke2).DefaultTimeout();
            Assert.False(invoke1.IsCompleted);

            await manager1.SetConnectionResultAsync(connection1.ConnectionId, CompletionMessage.WithResult(invocation1.InvocationId, 3));
            Assert.Equal(3, await invoke1.DefaultTimeout());
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task ClientDisconnectsWithoutCompletingClientResult()
    {
        var manager1 = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);

            await manager1.OnConnectedAsync(connection1).DefaultTimeout();

            var invoke1 = manager1.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cancellationToken: default);

            connection1.Abort();
            await manager1.OnDisconnectedAsync(connection1).DefaultTimeout();

            var ex = await Assert.ThrowsAsync<IOException>(() => invoke1).DefaultTimeout();
            Assert.Equal($"Connection '{connection1.ConnectionId}' disconnected.", ex.Message);
        }
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Fact]
    public async Task CanCancelClientResult()
    {
        var manager1 = CreateNewHubLifetimeManager();

        using (var client1 = new TestClient())
        {
            var connection1 = HubConnectionContextUtils.Create(client1.Connection);

            await manager1.OnConnectedAsync(connection1).DefaultTimeout();

            var cts = new CancellationTokenSource();
            var invoke1 = manager1.InvokeConnectionAsync<int>(connection1.ConnectionId, "Result", new object[] { "test" }, cts.Token);
            var invocation1 = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            cts.Cancel();

            await Assert.ThrowsAsync<HubException>(() => invoke1).DefaultTimeout();

            // Noop, just checking that it doesn't throw. This could be caused by an inflight response from a client while the server cancels the token
            await manager1.SetConnectionResultAsync(connection1.ConnectionId, CompletionMessage.WithResult(invocation1.InvocationId, 1));
        }
    }
}

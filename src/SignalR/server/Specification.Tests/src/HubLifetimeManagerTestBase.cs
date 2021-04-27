// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Extensions;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Specification.Tests
{
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

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                message = Assert.IsType<InvocationMessage>(client2.TryRead());
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

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
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

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
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

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
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

                var message = Assert.IsType<InvocationMessage>(client.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
            }
        }
    }
}

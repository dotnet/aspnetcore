// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Specification.Tests
{
    /// <summary>
    /// Base test class for lifetime manager implementations that support server scale-out.
    /// </summary>
    /// <typeparam name="TBackplane">An in-memory implementation of the backplane that <see cref="HubLifetimeManager{THub}"/>s communicate with.</typeparam>
    public abstract class ScaleoutHubLifetimeManagerTests<TBackplane> : HubLifetimeManagerTestsBase<Hub>
    {
        /// <summary>
        /// Method to create an implementation of an in-memory backplane for use in tests.
        /// </summary>
        /// <returns>The backplane implementation.</returns>
        public abstract TBackplane CreateBackplane();

        /// <summary>
        /// Method to create an implementation of <see cref="HubLifetimeManager{THub}"/> that uses the backplane from <see cref="CreateBackplane"/>.
        /// </summary>
        /// <param name="backplane">The backplane implementation for use in the <see cref="HubLifetimeManager{THub}"/>.</param>
        /// <returns></returns>
        public abstract HubLifetimeManager<Hub> CreateNewHubLifetimeManager(TBackplane backplane);

        private async Task AssertMessageAsync(TestClient client)
        {
            var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates two connections and assigns each to one of the
        /// lifetime managers, then tests that HubLifetimeManager.SendAllAsync from one lifetime manager will
        /// cause both clients to receive the message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeAllAsyncWithMultipleServersWritesToAllConnectionsOutput()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager1.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates two connections and assigns each to one of the
        /// lifetime managers, then disconnects one client and tests that HubLifetimeManager.SendAllAsync from one lifetime manager will
        /// only write to the connected client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeAllAsyncWithMultipleServersDoesNotWriteToDisconnectedConnectionsOutput()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager2.OnDisconnectedAsync(connection2).OrTimeout();

                await manager2.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);

                Assert.Null(client2.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates one connection and assigns it to one of the
        /// lifetime managers, then tests that HubLifetimeManager.SendConnectionAsync from the other lifetime manager
        /// writes to the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeConnectionAsyncOnServerWithoutConnectionWritesOutputToConnection()
        {
            var backplane = CreateBackplane();

            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates one connection and assigns it to one of the
        /// lifetime managers, then adds the connection to a group and tests that HubLifetimeManager.SendGroupAsync
        /// from the other lifetime manager writes to the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeGroupAsyncOnServerWithoutConnectionWritesOutputToGroupConnection()
        {
            var backplane = CreateBackplane();

            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        /// <summary>
        /// Creates a backplane and one lifetime manager, creates one connection and assigns it to the
        /// lifetime manager, then adds the connection to a group and disconnects the connection. Then tests
        /// that HubLifetimeManager.SendGroupAsync does not write to the connection.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task DisconnectConnectionRemovesConnectionFromGroup()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.OnDisconnectedAsync(connection).OrTimeout();

                await manager.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                Assert.Null(client.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and one lifetime manager, creates one connection and assigns it to the
        /// lifetime manager, then removes the connection from a group that it isn't in.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task RemoveGroupFromLocalConnectionNotInGroupDoesNothing()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.RemoveFromGroupAsync(connection.ConnectionId, "name").OrTimeout();
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates one connection and assigns it one of the
        /// lifetime managers, then removes the connection from a group with the other lifetime manager.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task RemoveGroupFromConnectionOnDifferentServerNotInGroupDoesNothing()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.RemoveFromGroupAsync(connection.ConnectionId, "name").OrTimeout();
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates one connection and assigns it to one of the
        /// lifetime managers, then adds the connection to a group via the other lifetime manager
        /// and tests that HubLifetimeManager.SendGroupAsync writes to the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task AddGroupAsyncForConnectionOnDifferentServerWorks()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        /// <summary>
        /// Creates a backplane and a lifetime manager, creates a connection and assigns it to the
        /// lifetime manager, then adds the connection to the same group twice
        /// and tests that HubLifetimeManager.SendGroupAsync writes to the client once.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task AddGroupAsyncForLocalConnectionAlreadyInGroupDoesNothing()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates a connection and assigns it to one of the
        /// lifetime managers, then adds the connection to the same group with both lifetime managers
        /// and tests that HubLifetimeManager.SendGroupAsync writes to the client once.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task AddGroupAsyncForConnectionOnDifferentServerAlreadyInGroupDoesNothing()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager2.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates a connection and assigns it to one of the
        /// lifetime managers, then adds the connection to a group and removes the connection from the group with
        /// the other lifetime manager and tests that HubLifetimeManager.SendGroupAsync does not write to the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task RemoveGroupAsyncForConnectionOnDifferentServerWorks()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddToGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);

                await manager2.RemoveFromGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                Assert.Null(client.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates a connection and assigns it to both of the
        /// lifetime managers, then sends to the connection from one of the lifetime managers to test that the message
        /// does not go over the backplane for local connections.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeConnectionAsyncForLocalConnectionDoesNotPublishToBackplane()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection).OrTimeout();
                await manager2.OnConnectedAsync(connection).OrTimeout();

                await manager1.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        /// <summary>
        /// Creates a backplane and two lifetime managers, creates a connection and assigns it to one of the
        /// lifetime managers, then forces a connection error when sending to the client from the other lifetime manager
        /// and checks that an exception isn't thrown.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task WritingToRemoteConnectionThatFailsDoesNotThrow()
        {
            var backplane = CreateBackplane();
            var manager1 = CreateNewHubLifetimeManager(backplane);
            var manager2 = CreateNewHubLifetimeManager(backplane);

            using (var client = new TestClient())
            {
                // Force an exception when writing to connection
                var connectionMock = HubConnectionContextUtils.CreateMock(client.Connection);

                await manager2.OnConnectedAsync(connectionMock).OrTimeout();

                // This doesn't throw because there is no connection.ConnectionId on this server so it has to publish to the backplane.
                // And once that happens there is no way to know if the invocation was successful or not.
                await manager1.SendConnectionAsync(connectionMock.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();
            }
        }

        /// <summary>
        /// Creates a backplane and a lifetime manager, creates two connections and assigns them to the
        /// lifetime manager, then forces an error when sending to one of the clients and verifies that the second client
        /// still receives the message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task WritingToGroupWithOneConnectionFailingSecondConnectionStillReceivesMessage()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                // Force an exception when writing to connection
                var connectionMock = HubConnectionContextUtils.CreateMock(client1.Connection);

                var connection1 = connectionMock;
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddToGroupAsync(connection1.ConnectionId, "group");
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "group");

                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                // connection1 will throw when receiving a group message, we are making sure other connections
                // are not affected by another connection throwing
                await AssertMessageAsync(client2);

                // Repeat to check that group can still be sent to
                await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client2);
            }
        }

        /// <summary>
        /// Creates a backplane and a lifetime manager, creates a few connections with user identifiers
        /// and assigns them to the lifetime manager, then tests that HubLifetimeManager.SendUserAsync only sends
        /// to the connections with the specified user identifier.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task InvokeUserSendsToAllConnectionsForUser()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "userA");
                var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "userA");
                var connection3 = HubConnectionContextUtils.Create(client3.Connection, userIdentifier: "userB");

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.OnConnectedAsync(connection3).OrTimeout();

                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        /// <summary>
        /// Creates a backplane and a lifetime manager, creates a few connections with user identifiers
        /// and assigns them to the lifetime manager, then disconnects one of the connections and tests that
        /// HubLifetimeManager.SendUserAsync still sends to the connections with the specified user identifier
        /// minus the disconnected connection.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
        [Fact]
        public async Task StillSubscribedToUserAfterOneOfMultipleConnectionsAssociatedWithUserDisconnects()
        {
            var backplane = CreateBackplane();
            var manager = CreateNewHubLifetimeManager(backplane);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "userA");
                var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "userA");
                var connection3 = HubConnectionContextUtils.Create(client3.Connection, userIdentifier: "userB");

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.OnConnectedAsync(connection3).OrTimeout();

                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);

                // Disconnect one connection for the user
                await manager.OnDisconnectedAsync(connection1).OrTimeout();
                await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client2);
            }
        }
    }
}

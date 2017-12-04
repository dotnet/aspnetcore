// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    public class RedisHubLifetimeManagerTests
    {
        [Fact]
        public async Task InvokeAllAsyncWritesToAllConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.OnDisconnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();
                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsyncWritesToAllConnectionsInGroupOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();

                await manager.InvokeGroupAsync("gunit", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();
                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncWritesToConnectionOutput()
        {
            using (var client = new TestClient())
            {
                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncOnNonExistentConnectionDoesNotThrow()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            await manager.InvokeConnectionAsync("NotARealConnectionId", "Hello", new object[] { "World" }).OrTimeout();
        }

        [Fact]
        public async Task InvokeAllAsyncWithMultipleServersWritesToAllConnectionsOutput()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager1.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeAllAsyncWithMultipleServersDoesNotWriteToDisconnectedConnectionsOutput()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager2.OnDisconnectedAsync(connection2).OrTimeout();

                await manager2.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();
                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncOnServerWithoutConnectionWritesOutputToConnection()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task InvokeGroupAsyncOnServerWithoutConnectionWritesOutputToGroupConnection()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task DisconnectConnectionRemovesConnectionFromGroup()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.OnDisconnectedAsync(connection).OrTimeout();

                await manager.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task RemoveGroupFromLocalConnectionNotInGroupDoesNothing()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.RemoveGroupAsync(connection.ConnectionId, "name").OrTimeout();
            }
        }

        [Fact]
        public async Task RemoveGroupFromConnectionOnDifferentServerNotInGroupDoesNothing()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
            Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.RemoveGroupAsync(connection.ConnectionId, "name").OrTimeout();
            }
        }

        [Fact]
        public async Task AddGroupAsyncForConnectionOnDifferentServerWorks()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task AddGroupAsyncForLocalConnectionAlreadyInGroupDoesNothing()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                await AssertMessageAsync(client);
                await connection.DisposeAsync().OrTimeout();
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task AddGroupAsyncForConnectionOnDifferentServerAlreadyInGroupDoesNothing()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager2.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                await AssertMessageAsync(client);
                await connection.DisposeAsync().OrTimeout();
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task RemoveGroupAsyncForConnectionOnDifferentServerWorks()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client);

                await manager2.RemoveGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncForLocalConnectionDoesNotPublishToRedis()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection).OrTimeout();
                await manager2.OnConnectedAsync(connection).OrTimeout();

                await manager1.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task WritingToRemoteConnectionThatFailsDoesNotThrow()
        {
            var manager1 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));
            var manager2 = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                // Force an exception when writing to connection
                var writer = new Mock<ChannelWriter<HubMessage>>();
                writer.Setup(o => o.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception());

                var connection = HubConnectionContextUtils.Create(client.Connection, new MockChannel(writer.Object));

                await manager2.OnConnectedAsync(connection).OrTimeout();

                // This doesn't throw because there is no connection.ConnectionId on this server so it has to publish to redis.
                // And once that happens there is no way to know if the invocation was successful or not.
                await manager1.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();
            }
        }

        [Fact]
        public async Task WritingToLocalConnectionThatFailsThrowsException()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client = new TestClient())
            {
                // Force an exception when writing to connection
                var writer = new Mock<ChannelWriter<HubMessage>>();
                writer.Setup(o => o.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception("Message"));

                var connection = HubConnectionContextUtils.Create(client.Connection, new MockChannel(writer.Object));

                await manager.OnConnectedAsync(connection).OrTimeout();

                var exception = await Assert.ThrowsAsync<Exception>(() => manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout());
                Assert.Equal("Message", exception.Message);
            }
        }

        [Fact]
        public async Task WritingToGroupWithOneConnectionFailingSecondConnectionStillReceivesMessage()
        {
            var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(), Options.Create(new RedisOptions()
            {
                Factory = t => new TestConnectionMultiplexer()
            }));

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                // Force an exception when writing to connection
                var writer = new Mock<ChannelWriter<HubMessage>>();
                writer.Setup(o => o.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception());

                var connection1 = HubConnectionContextUtils.Create(client1.Connection, new MockChannel(writer.Object));
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddGroupAsync(connection1.ConnectionId, "group");
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.AddGroupAsync(connection2.ConnectionId, "group");

                await manager.InvokeGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                // connection1 will throw when receiving a group message, we are making sure other connections
                // are not affected by another connection throwing
                await AssertMessageAsync(client2);

                // Repeat to check that group can still be sent to
                await manager.InvokeGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                await AssertMessageAsync(client2);
            }
        }

        private async Task AssertMessageAsync(TestClient client)
        {
            var message = Assert.IsType<InvocationMessage>(await client.ReadAsync());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }

        private class MyHub : Hub
        {
        }

        private class MockChannel : Channel<HubMessage>
        {
            public MockChannel(ChannelWriter<HubMessage> writer = null)
            {
                Writer = writer;
            }
        }
    }
}

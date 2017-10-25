// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.Logging;
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output1);
                AssertMessage(output2);
            }
        }

        [Fact]
        public async Task InvokeAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.OnDisconnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeGroupAsyncWritesToAllConnectionsInGroupOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();

                await manager.InvokeGroupAsync("gunit", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncWritesToConnectionOutput()
        {
            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();
                var manager = new RedisHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<RedisHubLifetimeManager<MyHub>>(),
                Options.Create(new RedisOptions()
                {
                    Factory = t => new TestConnectionMultiplexer()
                }));
                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager1.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output1);
                AssertMessage(output2);
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager2.OnDisconnectedAsync(connection2).OrTimeout();

                await manager2.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.OnDisconnectedAsync(connection).OrTimeout();

                await manager.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                Assert.False(output.In.TryRead(out var item));
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager2.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
                Assert.False(output.In.TryRead(out var item));
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();
                await manager2.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
                Assert.False(output.In.TryRead(out var item));
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection).OrTimeout();

                await manager1.AddGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);

                await manager2.RemoveGroupAsync(connection.ConnectionId, "name").OrTimeout();

                await manager2.InvokeGroupAsync("name", "Hello", new object[] { "World" }).OrTimeout();

                Assert.False(output.In.TryRead(out var item));
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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection).OrTimeout();
                await manager2.OnConnectedAsync(connection).OrTimeout();

                await manager1.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                AssertMessage(output);
                Assert.False(output.In.TryRead(out var item));
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
                var output = new Mock<Channel<HubMessage>>();
                output.Setup(o => o.Out.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception());

                var connection = new HubConnectionContext(output.Object, client.Connection);

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
                var output = new Mock<Channel<HubMessage>>();
                output.Setup(o => o.Out.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception("Message"));

                var connection = new HubConnectionContext(output.Object, client.Connection);

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
                var output2 = Channel.CreateUnbounded<HubMessage>();

                // Force an exception when writing to connection
                var output = new Mock<Channel<HubMessage>>();
                output.Setup(o => o.Out.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception());

                var connection1 = new HubConnectionContext(output.Object, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddGroupAsync(connection1.ConnectionId, "group");
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.AddGroupAsync(connection2.ConnectionId, "group");

                await manager.InvokeGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                // connection1 will throw when receiving a group message, we are making sure other connections
                // are not affected by another connection throwing
                AssertMessage(output2);

                // Repeat to check that group can still be sent to
                await manager.InvokeGroupAsync("group", "Hello", new object[] { "World" }).OrTimeout();
                AssertMessage(output2);
            }
        }

        private void AssertMessage(Channel<HubMessage> channel)
        {
            Assert.True(channel.In.TryRead(out var item));
            var message = Assert.IsType<InvocationMessage>(item);
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }

        private class MyHub : Hub
        {
        }
    }
}

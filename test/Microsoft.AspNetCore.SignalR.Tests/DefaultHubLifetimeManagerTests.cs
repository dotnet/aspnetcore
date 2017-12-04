using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultHubLifetimeManagerTests
    {
        [Fact]
        public async Task InvokeAllAsyncWritesToAllConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();

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

        [Fact]
        public async Task InvokeAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.OnDisconnectedAsync(connection2).OrTimeout();

                await manager.InvokeAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsyncWritesToAllConnectionsInGroupOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();

                await manager.InvokeGroupAsync("gunit", "Hello", new object[] { "World" }).OrTimeout();

                await connection1.DisposeAsync().OrTimeout();
                await connection2.DisposeAsync().OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncWritesToConnectionOutput()
        {
            using (var client = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                await connection.DisposeAsync().OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncThrowsIfConnectionFailsToWrite()
        {
            using (var client = new TestClient())
            {
                // Force an exception when writing to connection
                var writer = new Mock<ChannelWriter<HubMessage>>();
                writer.Setup(o => o.WaitToWriteAsync(It.IsAny<CancellationToken>())).Throws(new Exception("Message"));

                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection = HubConnectionContextUtils.Create(client.Connection, new MockChannel(writer.Object));

                await manager.OnConnectedAsync(connection).OrTimeout();

                var exception = await Assert.ThrowsAsync<Exception>(() => manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout());
                Assert.Equal("Message", exception.Message);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.InvokeConnectionAsync("NotARealConnectionId", "Hello", new object[] { "World" }).OrTimeout();
        }

        [Fact]
        public async Task AddGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.AddGroupAsync("NotARealConnectionId", "MyGroup").OrTimeout();
        }

        [Fact]
        public async Task RemoveGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.RemoveGroupAsync("NotARealConnectionId", "MyGroup").OrTimeout();
        }

        private class MyHub : Hub
        {

        }

        private class MockChannel: Channel<HubMessage>
        {

            public MockChannel(ChannelWriter<HubMessage> writer = null)
            {
                Writer = writer;
            }
        }
    }
}

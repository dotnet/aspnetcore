using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultHubLifetimeManagerTests
    {
        [Fact]
        public async Task SendAllAsyncWritesToAllConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

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
        public async Task SendAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.OnDisconnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task SendGroupAsyncWritesToAllConnectionsInGroupOutput()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddToGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();

                await manager.SendGroupAsync("gunit", "Hello", new object[] { "World" }).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task SendGroupExceptAsyncDoesNotWriteToExcludedConnections()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.AddToGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "gunit").OrTimeout();

                await manager.SendGroupExceptAsync("gunit", "Hello", new object[] { "World" }, new []{ connection2.ConnectionId }).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task SendConnectionAsyncWritesToConnectionOutput()
        {
            using (var client = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(client.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
            }
        }

        [Fact]
        public async Task SendConnectionAsyncOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
            await manager.SendConnectionAsync("NotARealConnectionId", "Hello", new object[] { "World" }).OrTimeout();
        }

        [Fact]
        public async Task AddGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
            await manager.AddToGroupAsync("NotARealConnectionId", "MyGroup").OrTimeout();
        }

        [Fact]
        public async Task RemoveGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
            await manager.RemoveFromGroupAsync("NotARealConnectionId", "MyGroup").OrTimeout();
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

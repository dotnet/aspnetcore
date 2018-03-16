using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit").OrTimeout();

                await manager.SendGroupAsync("gunit", "Hello", new object[] { "World" }).OrTimeout();

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
        public async Task SendConnectionAsyncDoesNotThrowIfConnectionFailsToWrite()
        {
            using (var client = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));

                var connectionMock = HubConnectionContextUtils.CreateMock(client.Connection);
                // Force an exception when writing to connection
                connectionMock.Setup(m => m.WriteAsync(It.IsAny<HubMessage>())).Throws(new Exception("Message"));
                var connection = connectionMock.Object;

                await manager.OnConnectedAsync(connection).OrTimeout();

                await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).OrTimeout();
            }
        }

        [Fact]
        public async Task SendAllAsyncSendsToAllConnectionsEvenWhenSomeFailToSend()
        {
            using (var client = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));

                var connectionMock = HubConnectionContextUtils.CreateMock(client.Connection);
                var connectionMock2 = HubConnectionContextUtils.CreateMock(client2.Connection);

                var tcs = new TaskCompletionSource<object>();
                var tcs2 = new TaskCompletionSource<object>();
                // Force an exception when writing to connection
                connectionMock.Setup(m => m.WriteAsync(It.IsAny<HubMessage>())).Callback(() => tcs.TrySetResult(null)).Throws(new Exception("Message"));
                connectionMock2.Setup(m => m.WriteAsync(It.IsAny<HubMessage>())).Callback(() => tcs2.TrySetResult(null)).Throws(new Exception("Message"));
                var connection = connectionMock.Object;
                var connection2 = connectionMock2.Object;

                await manager.OnConnectedAsync(connection).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                // Check that all connections were "written" to
                await tcs.Task.OrTimeout();
                await tcs2.Task.OrTimeout();
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
            await manager.AddGroupAsync("NotARealConnectionId", "MyGroup").OrTimeout();
        }

        [Fact]
        public async Task RemoveGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
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

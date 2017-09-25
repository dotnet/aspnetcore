using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.InvokeAllAsync("Hello", new object[] { "World" });

                Assert.True(output1.In.TryRead(out var item));
                var message = item as InvocationMessage;
                Assert.NotNull(message);
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.True(output2.In.TryRead(out item));
                message = item as InvocationMessage;
                Assert.NotNull(message);
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.OnDisconnectedAsync(connection2);

                await manager.InvokeAllAsync("Hello", new object[] { "World" });

                Assert.True(output1.In.TryRead(out var item));
                var message = item as InvocationMessage;
                Assert.NotNull(message);
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.False(output2.In.TryRead(out item));
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

                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit");

                await manager.InvokeGroupAsync("gunit", "Hello", new object[] { "World" });

                Assert.True(output1.In.TryRead(out var item));
                var message = item as InvocationMessage;
                Assert.NotNull(message);
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);

                Assert.False(output2.In.TryRead(out item));
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncWritesToConnectionOutput()
        {
            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();
                var manager = new DefaultHubLifetimeManager<MyHub>();
                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                Assert.True(output.In.TryRead(out var item));
                var message = item as InvocationMessage;
                Assert.NotNull(message);
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsyncOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.InvokeConnectionAsync("NotARealConnectionId", "Hello", new object[] { "World" });
        }

        [Fact]
        public async Task AddGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.AddGroupAsync("NotARealConnectionId", "MyGroup");
        }

        [Fact]
        public async Task RemoveGroupOnNonExistentConnectionNoops()
        {
            var manager = new DefaultHubLifetimeManager<MyHub>();
            await manager.RemoveGroupAsync("NotARealConnectionId", "MyGroup");
        }

        private class MyHub : Hub
        {

        }
    }
}

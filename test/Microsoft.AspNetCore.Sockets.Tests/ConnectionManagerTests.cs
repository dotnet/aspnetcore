using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void ReservedConnectionsHaveConnectionId()
        {
            var connectionManager = new ConnectionManager();
            var state = connectionManager.ReserveConnection();

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.True(state.Active);
            Assert.Null(state.Close);
            Assert.Null(state.Connection.Channel);
        }

        [Fact]
        public void ReservedConnectionsCanBeRetrieved()
        {
            var connectionManager = new ConnectionManager();
            var state = connectionManager.ReserveConnection();

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);

            ConnectionState newState;
            Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            Assert.Same(newState, state);
        }

        [Fact]
        public void AddNewConnection()
        {
            using (var factory = new ChannelFactory())
            using (var channel = new HttpChannel(factory))
            {
                var connectionManager = new ConnectionManager();
                var state = connectionManager.AddNewConnection(channel);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(state.Connection.Channel);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(channel, newState.Connection.Channel);
            }
        }

        [Fact]
        public void RemoveConnection()
        {
            using (var factory = new ChannelFactory())
            using (var channel = new HttpChannel(factory))
            {
                var connectionManager = new ConnectionManager();
                var state = connectionManager.AddNewConnection(channel);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(state.Connection.Channel);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(channel, newState.Connection.Channel);

                connectionManager.RemoveConnection(state.Connection.ConnectionId);
                Assert.False(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            }
        }
    }
}

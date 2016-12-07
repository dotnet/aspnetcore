// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void NewConnectionsHaveConnectionId()
        {
            using (var factory = new PipelineFactory())
            {
                var connectionManager = new ConnectionManager(factory);
                var state = connectionManager.CreateConnection(ConnectionMode.Streaming);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.True(state.Active);
                Assert.Null(state.Close);
                Assert.NotNull(((StreamingConnectionState)state).Connection.Transport);
            }
        }

        [Fact]
        public void NewConnectionsCanBeRetrieved()
        {
            using (var factory = new PipelineFactory())
            {
                var connectionManager = new ConnectionManager(factory);
                var state = connectionManager.CreateConnection(ConnectionMode.Streaming);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
            }
        }

        [Fact]
        public void AddNewConnection()
        {
            using (var factory = new PipelineFactory())
            {
                var connectionManager = new ConnectionManager(factory);
                var state = connectionManager.CreateConnection(ConnectionMode.Streaming);

                var transport = ((StreamingConnectionState)state).Connection.Transport;

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(transport);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(transport, ((StreamingConnectionState)newState).Connection.Transport);
            }
        }

        [Fact]
        public void RemoveConnection()
        {
            using (var factory = new PipelineFactory())
            {
                var connectionManager = new ConnectionManager(factory);
                var state = connectionManager.CreateConnection(ConnectionMode.Streaming);

                var transport = ((StreamingConnectionState)state).Connection.Transport;

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(transport);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(transport, ((StreamingConnectionState)newState).Connection.Transport);

                connectionManager.RemoveConnection(state.Connection.ConnectionId);
                Assert.False(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            }
        }

        [Fact]
        public async Task CloseConnectionsEndsAllPendingConnections()
        {
            using (var factory = new PipelineFactory())
            {
                var connectionManager = new ConnectionManager(factory);
                var state = (StreamingConnectionState)connectionManager.CreateConnection(ConnectionMode.Streaming);

                var task = Task.Run(async () =>
                {
                    var result = await state.Connection.Transport.Input.ReadAsync();

                    Assert.True(result.IsCompleted);
                });

                connectionManager.CloseConnections();

                await task;
            }
        }
    }
}

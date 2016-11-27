// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void ReservedConnectionsHaveConnectionId()
        {
            var lifetime = new ApplicationLifetime();
            var connectionManager = new ConnectionManager(lifetime);
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
            var lifetime = new ApplicationLifetime();
            var connectionManager = new ConnectionManager(lifetime);
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
            using (var factory = new PipelineFactory())
            using (var connection = new HttpConnection(factory))
            {
                var lifetime = new ApplicationLifetime();
                var connectionManager = new ConnectionManager(lifetime);
                var state = connectionManager.AddNewConnection(connection);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(state.Connection.Channel);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(connection, newState.Connection.Channel);
            }
        }

        [Fact]
        public void RemoveConnection()
        {
            using (var factory = new PipelineFactory())
            using (var connection = new HttpConnection(factory))
            {
                var lifetime = new ApplicationLifetime();
                var connectionManager = new ConnectionManager(lifetime);
                var state = connectionManager.AddNewConnection(connection);

                Assert.NotNull(state.Connection);
                Assert.NotNull(state.Connection.ConnectionId);
                Assert.NotNull(state.Connection.Channel);

                ConnectionState newState;
                Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
                Assert.Same(newState, state);
                Assert.Same(connection, newState.Connection.Channel);

                connectionManager.RemoveConnection(state.Connection.ConnectionId);
                Assert.False(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            }
        }

        [Fact]
        public async Task ApplicationStoppingClosesConnections()
        {
            using (var factory = new PipelineFactory())
            using (var connection = new HttpConnection(factory))
            {
                var lifetime = new ApplicationLifetime();
                var connectionManager = new ConnectionManager(lifetime);
                var state = connectionManager.AddNewConnection(connection);

                var task = Task.Run(async () =>
                {
                    var result = await connection.Input.ReadAsync();

                    Assert.True(result.IsCompleted);
                });

                lifetime.StopApplication();

                await task;
            }
        }
    }
}

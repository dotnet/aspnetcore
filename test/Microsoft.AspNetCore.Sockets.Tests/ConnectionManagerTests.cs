// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void NewConnectionsHaveConnectionId()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.Equal(ConnectionState.ConnectionStatus.Inactive, state.Status);
            Assert.Null(state.ApplicationTask);
            Assert.Null(state.TransportTask);
            Assert.Null(state.Cancellation);
            Assert.Null(state.RequestId);
            Assert.NotNull(state.Connection.Transport);
        }

        [Fact]
        public void NewConnectionsCanBeRetrieved()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);

            ConnectionState newState;
            Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            Assert.Same(newState, state);
        }

        [Fact]
        public void AddNewConnection()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();

            var transport = state.Connection.Transport;

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.NotNull(transport);

            ConnectionState newState;
            Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            Assert.Same(newState, state);
            Assert.Same(transport, newState.Connection.Transport);
        }

        [Fact]
        public void RemoveConnection()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();

            var transport = state.Connection.Transport;

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.NotNull(transport);

            ConnectionState newState;
            Assert.True(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
            Assert.Same(newState, state);
            Assert.Same(transport, newState.Connection.Transport);

            connectionManager.RemoveConnection(state.Connection.ConnectionId);
            Assert.False(connectionManager.TryGetConnection(state.Connection.ConnectionId, out newState));
        }

        [Fact]
        public async Task CloseConnectionsEndsAllPendingConnections()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();

            state.ApplicationTask = Task.Run(async () =>
            {
                Assert.False(await state.Connection.Transport.Input.WaitToReadAsync());
            });

            state.TransportTask = Task.Run(async () =>
            {
                Assert.False(await state.Application.Input.WaitToReadAsync());
            });

            connectionManager.CloseConnections();

            await state.DisposeAsync();
        }

        [Fact]
        public async Task DisposeInactiveConnection()
        {
            var connectionManager = CreateConnectionManager();
            var state = connectionManager.CreateConnection();;

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.NotNull(state.Connection.Transport);

            await state.DisposeAsync();
            Assert.Equal(state.Status, ConnectionState.ConnectionStatus.Disposed);
        }

        private static ConnectionManager CreateConnectionManager()
        {
            return new ConnectionManager(new Logger<ConnectionManager>(new LoggerFactory()));
        }
    }
}

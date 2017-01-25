// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            var connectionManager = new ConnectionManager();
            var state = connectionManager.CreateConnection();

            Assert.NotNull(state.Connection);
            Assert.NotNull(state.Connection.ConnectionId);
            Assert.True(state.Active);
            Assert.Null(state.ApplicationTask);
            Assert.Null(state.TransportTask);
            Assert.NotNull(state.Connection.Transport);
        }

        [Fact]
        public void NewConnectionsCanBeRetrieved()
        {
            var connectionManager = new ConnectionManager();
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
            var connectionManager = new ConnectionManager();
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
            var connectionManager = new ConnectionManager();
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
            var connectionManager = new ConnectionManager();
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
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
            var connection = connectionManager.CreateConnection();

            Assert.NotNull(connection.ConnectionId);
            Assert.Equal(DefaultConnectionContext.ConnectionStatus.Inactive, connection.Status);
            Assert.Null(connection.ApplicationTask);
            Assert.Null(connection.TransportTask);
            Assert.Null(connection.Cancellation);
            Assert.NotEqual(default, connection.LastSeenUtc);
            Assert.Null(connection.Transport);
        }

        [Fact]
        public void NewConnectionsCanBeRetrieved()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection();

            Assert.NotNull(connection.ConnectionId);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionId, out var newConnection));
            Assert.Same(newConnection, connection);
        }

        [Fact]
        public void AddNewConnection()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionId, out var newConnection));
            Assert.Same(newConnection, connection);
            Assert.Same(transport, newConnection.Transport);
        }

        [Fact]
        public void RemoveConnection()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionId, out var newConnection));
            Assert.Same(newConnection, connection);
            Assert.Same(transport, newConnection.Transport);

            connectionManager.RemoveConnection(connection.ConnectionId);
            Assert.False(connectionManager.TryGetConnection(connection.ConnectionId, out newConnection));
        }

        [Fact]
        public async Task CloseConnectionsEndsAllPendingConnections()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

            connection.ApplicationTask = Task.Run(async () =>
            {
                var result = await connection.Transport.Input.ReadAsync();

                try
                {
                    Assert.True(result.IsCompleted);
                }
                finally
                {
                    connection.Transport.Input.AdvanceTo(result.Buffer.End);
                }
            });

            connection.TransportTask = Task.Run(async () =>
            {
                var result = await connection.Application.Input.ReadAsync();
                try
                {
                    Assert.True(result.IsCompleted);
                }
                finally
                {
                    connection.Application.Input.AdvanceTo(result.Buffer.End);
                }
            });

            connectionManager.CloseConnections();

            await connection.DisposeAsync();
        }

        [Fact]
        public async Task DisposingConnectionMultipleTimesWaitsOnConnectionClose()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetResult(null);

            await Task.WhenAll(firstTask, secondTask).OrTimeout();
        }

        [Fact]
        public async Task DisposingConnectionMultipleGetsExceptionFromTransportOrApp()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetException(new InvalidOperationException("Error"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await firstTask.OrTimeout());
            Assert.Equal("Error", exception.Message);

            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await secondTask.OrTimeout());
            Assert.Equal("Error", exception.Message);
        }

        [Fact]
        public async Task DisposingConnectionMultipleGetsCancellation()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetCanceled();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await firstTask.OrTimeout());
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await secondTask.OrTimeout());
        }

        [Fact]
        public async Task DisposeInactiveConnection()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(connection.Transport);

            await connection.DisposeAsync();
            Assert.Equal(DefaultConnectionContext.ConnectionStatus.Disposed, connection.Status);
        }

        [Fact]
        public void ScanAfterDisposeNoops()
        {
            var connectionManager = CreateConnectionManager();
            var connection = connectionManager.CreateConnection();

            connectionManager.CloseConnections();

            connectionManager.Scan();
        }

        [Fact]
        public async Task ApplicationLifetimeIsHookedUp()
        {
            var appLifetime = new TestApplicationLifetime();
            var connectionManager = CreateConnectionManager(appLifetime);
            var tcs = new TaskCompletionSource<object>();

            appLifetime.Start();

            var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

            connection.Application.Output.OnReaderCompleted((error, state) =>
            {
                tcs.TrySetResult(null);
            },
            null);

            appLifetime.StopApplication();

            // Connection should be disposed so this should complete immediately
            await tcs.Task.OrTimeout();
        }

        private static ConnectionManager CreateConnectionManager(IApplicationLifetime lifetime = null)
        {
            lifetime = lifetime ?? new EmptyApplicationLifetime();
            return new ConnectionManager(new Logger<ConnectionManager>(new LoggerFactory()), lifetime);
        }
    }
}

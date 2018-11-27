// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class HttpConnectionManagerTests : VerifiableLoggedTest
    {
        [Fact]
        public void NewConnectionsHaveConnectionId()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection();

                Assert.NotNull(connection.ConnectionId);
                Assert.Equal(HttpConnectionStatus.Inactive, connection.Status);
                Assert.Null(connection.ApplicationTask);
                Assert.Null(connection.TransportTask);
                Assert.Null(connection.Cancellation);
                Assert.NotEqual(default, connection.LastSeenUtc);
                Assert.NotNull(connection.Transport);
                Assert.NotNull(connection.Application);
            }
        }

        [Theory]
        [InlineData(ConnectionStates.ClosedUngracefully | ConnectionStates.ApplicationNotFaulted | ConnectionStates.TransportNotFaulted)]
        [InlineData(ConnectionStates.ClosedUngracefully | ConnectionStates.ApplicationNotFaulted | ConnectionStates.TransportFaulted)]
        [InlineData(ConnectionStates.ClosedUngracefully | ConnectionStates.ApplicationFaulted | ConnectionStates.TransportFaulted)]
        [InlineData(ConnectionStates.ClosedUngracefully | ConnectionStates.ApplicationFaulted | ConnectionStates.TransportNotFaulted)]

        [InlineData(ConnectionStates.CloseGracefully | ConnectionStates.ApplicationNotFaulted | ConnectionStates.TransportNotFaulted)]
        [InlineData(ConnectionStates.CloseGracefully | ConnectionStates.ApplicationNotFaulted | ConnectionStates.TransportFaulted)]
        [InlineData(ConnectionStates.CloseGracefully | ConnectionStates.ApplicationFaulted | ConnectionStates.TransportFaulted)]
        [InlineData(ConnectionStates.CloseGracefully | ConnectionStates.ApplicationFaulted | ConnectionStates.TransportNotFaulted)]
        public async Task DisposingConnectionsClosesBothSidesOfThePipe(ConnectionStates states)
        {
            using (StartVerifiableLog())
            {
                var closeGracefully = (states & ConnectionStates.CloseGracefully) != 0;
                var applicationFaulted = (states & ConnectionStates.ApplicationFaulted) != 0;
                var transportFaulted = (states & ConnectionStates.TransportFaulted) != 0;

                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection();

                if (applicationFaulted)
                {
                    // If the application is faulted then we want to make sure the transport task only completes after
                    // the application completes
                    connection.ApplicationTask = Task.FromException(new Exception("Application failed"));
                    connection.TransportTask = Task.Run(async () =>
                    {
                        // Wait for the application to end
                        var result = await connection.Application.Input.ReadAsync();
                        connection.Application.Input.AdvanceTo(result.Buffer.End);

                        if (transportFaulted)
                        {
                            throw new Exception("Transport failed");
                        }
                    });

                }
                else if (transportFaulted)
                {
                    // If the transport is faulted then we want to make sure the transport task only completes after
                    // the application completes
                    connection.TransportTask = Task.FromException(new Exception("Application failed"));
                    connection.ApplicationTask = Task.Run(async () =>
                    {
                        // Wait for the application to end
                        var result = await connection.Transport.Input.ReadAsync();
                        connection.Transport.Input.AdvanceTo(result.Buffer.End);
                    });
                }
                else
                {
                    connection.ApplicationTask = Task.CompletedTask;
                    connection.TransportTask = Task.CompletedTask;
                }

                var applicationInputTcs = new TaskCompletionSource<object>();
                var applicationOutputTcs = new TaskCompletionSource<object>();
                var transportInputTcs = new TaskCompletionSource<object>();
                var transportOutputTcs = new TaskCompletionSource<object>();

                connection.Transport.Input.OnWriterCompleted((_, __) => transportInputTcs.TrySetResult(null), null);
                connection.Transport.Output.OnReaderCompleted((_, __) => transportOutputTcs.TrySetResult(null), null);
                connection.Application.Input.OnWriterCompleted((_, __) => applicationInputTcs.TrySetResult(null), null);
                connection.Application.Output.OnReaderCompleted((_, __) => applicationOutputTcs.TrySetResult(null), null);

                try
                {
                    await connection.DisposeAsync(closeGracefully).OrTimeout();
                }
                catch (Exception ex) when (!(ex is TimeoutException))
                {
                    // Ignore the exception that bubbles out of the failing task
                }

                await Task.WhenAll(applicationInputTcs.Task, applicationOutputTcs.Task, transportInputTcs.Task, transportOutputTcs.Task).OrTimeout();
            }
        }

        [Fact]
        public void NewConnectionsCanBeRetrieved()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection();

                Assert.NotNull(connection.ConnectionId);

                Assert.True(connectionManager.TryGetConnection(connection.ConnectionId, out var newConnection));
                Assert.Same(newConnection, connection);
            }
        }

        [Fact]
        public void AddNewConnection()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

                var transport = connection.Transport;

                Assert.NotNull(connection.ConnectionId);
                Assert.NotNull(transport);

                Assert.True(connectionManager.TryGetConnection(connection.ConnectionId, out var newConnection));
                Assert.Same(newConnection, connection);
                Assert.Same(transport, newConnection.Transport);
            }
        }

        [Fact]
        public void RemoveConnection()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
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
        }

        [Fact]
        public async Task CloseConnectionsEndsAllPendingConnections()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

                connection.ApplicationTask = Task.Run(async () =>
                {
                    var result = await connection.Transport.Input.ReadAsync();

                    try
                    {
                        Assert.True(result.IsCompleted);

                        // We should be able to write
                        await connection.Transport.Output.WriteAsync(new byte[] { 1 });
                    }
                    finally
                    {
                        connection.Transport.Input.AdvanceTo(result.Buffer.End);
                    }
                });

                connection.TransportTask = Task.Run(async () =>
                {
                    var result = await connection.Application.Input.ReadAsync();
                    Assert.Equal(new byte[] { 1 }, result.Buffer.ToArray());
                    connection.Application.Input.AdvanceTo(result.Buffer.End);

                    result = await connection.Application.Input.ReadAsync();
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
        }

        [Fact]
        public async Task DisposingConnectionMultipleTimesWaitsOnConnectionClose()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
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
        }

        [Fact]
        public async Task DisposingConnectionMultipleGetsExceptionFromTransportOrApp()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
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
        }

        [Fact]
        public async Task DisposingConnectionMultipleGetsCancellation()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
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
        }

        [Fact]
        public async Task DisposeInactiveConnection()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection(PipeOptions.Default, PipeOptions.Default);

                Assert.NotNull(connection.ConnectionId);
                Assert.NotNull(connection.Transport);

                await connection.DisposeAsync();
                Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);
            }
        }

        [Fact]
        public async Task DisposeInactiveConnectionWithNoPipes()
        {
            using (StartVerifiableLog())
            {
                var connectionManager = CreateConnectionManager(LoggerFactory);
                var connection = connectionManager.CreateConnection();

                Assert.NotNull(connection.ConnectionId);
                Assert.NotNull(connection.Transport);
                Assert.NotNull(connection.Application);

                await connection.DisposeAsync();
                Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);
            }
        }

        [Fact]
        public async Task ApplicationLifetimeIsHookedUp()
        {
            using (StartVerifiableLog())
            {
                var appLifetime = new TestApplicationLifetime();
                var connectionManager = CreateConnectionManager(LoggerFactory, appLifetime);
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
        }

        [Fact]
        public async Task ApplicationLifetimeCanStartBeforeHttpConnectionManagerInitialized()
        {
            using (StartVerifiableLog())
            {
                var appLifetime = new TestApplicationLifetime();
                appLifetime.Start();

                var connectionManager = CreateConnectionManager(LoggerFactory, appLifetime);
                var tcs = new TaskCompletionSource<object>();

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
        }

        private static HttpConnectionManager CreateConnectionManager(ILoggerFactory loggerFactory, IApplicationLifetime lifetime = null)
        {
            lifetime = lifetime ?? new EmptyApplicationLifetime();
            return new HttpConnectionManager(loggerFactory, lifetime);
        }

        [Flags]
        public enum ConnectionStates
        {
            ClosedUngracefully = 1,
            ApplicationNotFaulted = 2,
            TransportNotFaulted = 4,
            ApplicationFaulted = 8,
            TransportFaulted = 16,
            CloseGracefully = 32
        }
    }
}

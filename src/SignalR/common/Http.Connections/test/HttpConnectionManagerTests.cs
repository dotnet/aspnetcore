// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class HttpConnectionManagerTests : VerifiableLoggedTest
{
    [Fact]
    public void HttpConnectionDispatcherOptionsDefaults()
    {
        var options = new HttpConnectionDispatcherOptions();
        Assert.Equal(TimeSpan.FromSeconds(10), options.TransportSendTimeout);
        Assert.Equal(65536, options.TransportMaxBufferSize);
        Assert.Equal(65536, options.ApplicationMaxBufferSize);
        Assert.Equal(HttpTransports.All, options.Transports);
        Assert.False(options.CloseOnAuthenticationExpiration);
    }

    [Fact]
    public void HttpConnectionDispatcherOptionsNegativeBufferSizeThrows()
    {
        var httpOptions = new HttpConnectionDispatcherOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => httpOptions.TransportMaxBufferSize = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => httpOptions.ApplicationMaxBufferSize = -1);
    }

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
            Assert.NotEqual(default, connection.LastSeenTicks);
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
                    return false;
                });

            }
            else if (transportFaulted)
            {
                // If the transport is faulted then we want to make sure the transport task only completes after
                // the application completes
                connection.TransportTask = Task.FromException<bool>(new Exception("Application failed"));
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
                connection.TransportTask = Task.FromResult(true);
            }

            try
            {
                await connection.DisposeAsync(closeGracefully).DefaultTimeout();
            }
            catch (Exception ex) when (!(ex is TimeoutException))
            {
                // Ignore the exception that bubbles out of the failing task
            }

            var result = await connection.Transport.Output.FlushAsync();
            Assert.True(result.IsCompleted);

            result = await connection.Application.Output.FlushAsync();
            Assert.True(result.IsCompleted);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await connection.Transport.Input.ReadAsync());
            Assert.Equal("Reading is not allowed after reader was completed.", exception.Message);

            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await connection.Application.Input.ReadAsync());
            Assert.Equal("Reading is not allowed after reader was completed.", exception.Message);
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

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionToken, out var newConnection));
            Assert.Same(newConnection, connection);
        }
    }

    [Fact]
    public void AddNewConnection()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection();
            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(connection.ConnectionToken);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionToken, out var newConnection));
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
            var connection = connectionManager.CreateConnection();

            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionToken, out var newConnection));
            Assert.Same(newConnection, connection);
            Assert.Same(transport, newConnection.Transport);

            connectionManager.RemoveConnection(connection.ConnectionToken, connection.TransportType, HttpConnectionStopStatus.Timeout);
            Assert.False(connectionManager.TryGetConnection(connection.ConnectionToken, out newConnection));
        }
    }

    [Fact]
    public void ConnectionIdAndConnectionTokenAreTheSameForNegotiateVersionZero()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection(new(), negotiateVersion: 0);

            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionToken, out var newConnection));
            Assert.Same(newConnection, connection);
            Assert.Same(transport, newConnection.Transport);
            Assert.Equal(connection.ConnectionId, connection.ConnectionToken);

        }
    }

    [Fact]
    public void ConnectionIdAndConnectionTokenAreDifferentForNegotiateVersionOne()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection(new(), negotiateVersion: 1);

            var transport = connection.Transport;

            Assert.NotNull(connection.ConnectionId);
            Assert.NotNull(transport);

            Assert.True(connectionManager.TryGetConnection(connection.ConnectionToken, out var newConnection));
            Assert.False(connectionManager.TryGetConnection(connection.ConnectionId, out var _));
            Assert.Same(newConnection, connection);
            Assert.Same(transport, newConnection.Transport);
            Assert.NotEqual(connection.ConnectionId, connection.ConnectionToken);

        }
    }

    [Fact]
    public async Task CloseConnectionsEndsAllPendingConnections()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection();

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
                    Assert.True(result.IsCanceled);
                }
                finally
                {
                    connection.Application.Input.AdvanceTo(result.Buffer.End);
                }
                return true;
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
            var connection = connectionManager.CreateConnection();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetResult(true);

            await Task.WhenAll(firstTask, secondTask).DefaultTimeout();
        }
    }

    [Fact]
    public async Task DisposingConnectionMultipleGetsExceptionFromTransportOrApp()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetException(new InvalidOperationException("Error"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await firstTask.DefaultTimeout());
            Assert.Equal("Error", exception.Message);

            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await secondTask.DefaultTimeout());
            Assert.Equal("Error", exception.Message);
        }
    }

    [Fact]
    public async Task DisposingConnectionMultipleGetsCancellation()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ApplicationTask = tcs.Task;
            connection.TransportTask = tcs.Task;

            var firstTask = connection.DisposeAsync();
            var secondTask = connection.DisposeAsync();
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);

            tcs.TrySetCanceled();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await firstTask.DefaultTimeout());
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await secondTask.DefaultTimeout());
        }
    }

    [Fact]
    public async Task DisposeInactiveConnection()
    {
        using (StartVerifiableLog())
        {
            var connectionManager = CreateConnectionManager(LoggerFactory);
            var connection = connectionManager.CreateConnection();

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

            appLifetime.Start();

            var connection = connectionManager.CreateConnection();

            appLifetime.StopApplication();

            var result = await connection.Application.Output.FlushAsync();
            Assert.True(result.IsCompleted);
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

            var connection = connectionManager.CreateConnection();

            appLifetime.StopApplication();

            var result = await connection.Application.Output.FlushAsync();
            Assert.True(result.IsCompleted);
        }
    }

    private static HttpConnectionManager CreateConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime lifetime = null, HttpConnectionsMetrics metrics = null)
    {
        lifetime ??= new EmptyApplicationLifetime();
        return new HttpConnectionManager(loggerFactory, lifetime, Options.Create(new ConnectionOptions()), metrics ?? new HttpConnectionsMetrics(new TestMeterFactory()));
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

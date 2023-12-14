// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Tests;

[NamedPipesSupported]
public class NamedPipeConnectionListenerTests : TestApplicationErrorLoggerLoggedTest
{
    [ConditionalFact]
    public async Task AcceptAsync_AfterUnbind_ReturnNull()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        await connectionListener.UnbindAsync().DefaultTimeout();

        // Assert
        Assert.Null(await connectionListener.AcceptAsync().DefaultTimeout());
    }

    private class TestObjectPoolProvider : ObjectPoolProvider
    {
        public List<ITestObjectPool> Pools { get; } = new List<ITestObjectPool>();

        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            var pool = new TestObjectPool<T>(policy);
            Pools.Add(pool);

            return pool;
        }

        private class TestObjectPool<T> : ObjectPool<T>, ITestObjectPool where T : class
        {
            private readonly IPooledObjectPolicy<T> _policy;

            public TestObjectPool(IPooledObjectPolicy<T> policy)
            {
                _policy = policy;
            }

            public int GetCount { get; private set; }
            public int ReturnSuccessCount { get; private set; }
            public int ReturnFailureCount { get; private set; }

            public override T Get()
            {
                GetCount++;
                return _policy.Create();
            }

            public override void Return(T obj)
            {
                if (_policy.Return(obj))
                {
                    ReturnSuccessCount++;
                }
                else
                {
                    ReturnFailureCount++;
                }
            }
        }
    }

    private interface ITestObjectPool
    {
        int GetCount { get; }
        int ReturnSuccessCount { get; }
        int ReturnFailureCount { get; }
    }

    [ConditionalFact]
    public async Task AcceptAsync_ClientCreatesConnection_ServerAccepts()
    {
        // Arrange
        var testObjectPoolProvider = new TestObjectPoolProvider();
        var options = new NamedPipeTransportOptions
        {
            ListenerQueueCount = 2
        };
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, options: options, objectPoolProvider: testObjectPoolProvider);
        var pool = Assert.Single(testObjectPoolProvider.Pools);
        Assert.Equal(options.ListenerQueueCount, pool.GetCount);

        // Stream 1
        var acceptTask1 = connectionListener.AcceptAsync();
        await using var clientStream1 = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        await clientStream1.ConnectAsync();

        var serverConnection1 = await acceptTask1.DefaultTimeout();
        Assert.False(serverConnection1.ConnectionClosed.IsCancellationRequested, "Connection 1 should be open");
        await serverConnection1.DisposeAsync().AsTask().DefaultTimeout();
        Assert.True(serverConnection1.ConnectionClosed.IsCancellationRequested, "Connection 1 should be closed");

        Assert.Equal(options.ListenerQueueCount + 1, pool.GetCount);
        Assert.Equal(1, pool.ReturnSuccessCount);
        Assert.Equal(0, pool.ReturnFailureCount);

        // Stream 2
        var acceptTask2 = connectionListener.AcceptAsync();
        await using var clientStream2 = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        await clientStream2.ConnectAsync();

        var serverConnection2 = await acceptTask2.DefaultTimeout();
        Assert.False(serverConnection2.ConnectionClosed.IsCancellationRequested, "Connection 2 should be open");
        await serverConnection2.DisposeAsync().AsTask().DefaultTimeout();
        Assert.True(serverConnection2.ConnectionClosed.IsCancellationRequested, "Connection 2 should be closed");

        Assert.Equal(options.ListenerQueueCount + 2, pool.GetCount);
        Assert.Equal(2, pool.ReturnSuccessCount);
        Assert.Equal(0, pool.ReturnFailureCount);
    }

    [ConditionalFact]
    public async Task AcceptAsync_UnbindAfterCall_CleanExit()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAsync();

        await connectionListener.UnbindAsync().DefaultTimeout();

        // Assert
        Assert.Null(await acceptTask.AsTask().DefaultTimeout());

        Assert.DoesNotContain(LogMessages, m => m.LogLevel >= LogLevel.Error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public async Task AcceptAsync_ParallelConnections_ClientConnectionsSuccessfullyAccepted(int listenerQueueCount)
    {
        // Arrange
        const int ParallelCount = 10;
        const int ParallelCallCount = 250;
        const int TotalCallCount = ParallelCount * ParallelCallCount;

        var currentCallCount = 0;
        var options = new NamedPipeTransportOptions();
        options.ListenerQueueCount = listenerQueueCount;
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, options: options);

        // Act
        var serverTask = Task.Run(async () =>
        {
            while (currentCallCount < TotalCallCount)
            {
                _ = await connectionListener.AcceptAsync();

                currentCallCount++;

                Logger.LogInformation($"Server accepted {currentCallCount} calls.");
            }

            Logger.LogInformation($"Server task complete.");
        });

        var cts = new CancellationTokenSource();
        var parallelTasks = new List<Task>();
        for (var i = 0; i < ParallelCount; i++)
        {
            parallelTasks.Add(Task.Run(async () =>
            {
                var clientStreamCount = 0;
                while (clientStreamCount < ParallelCallCount)
                {
                    try
                    {
                        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
                        await clientStream.ConnectAsync(cts.Token);

                        await clientStream.WriteAsync(new byte[1], cts.Token);
                        await clientStream.DisposeAsync();
                        clientStreamCount++;
                    }
                    catch (IOException ex)
                    {
                        Logger.LogInformation(ex, "Client exception.");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }));
        }

        await serverTask.DefaultTimeout();

        cts.Cancel();
        await Task.WhenAll(parallelTasks).DefaultTimeout();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Non-OS implementations use UDS with an unlimited accept limit.")]
    public async Task AcceptAsync_HitBacklogLimit_ClientConnectionsSuccessfullyAccepted()
    {
        // Arrange
        var options = new NamedPipeTransportOptions();
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, options: options);

        // Act
        var clients = new List<ClientStreamContext>();
        var connectBlocked = false;
        for (var i = 0; i < 100; i++)
        {
            Logger.LogInformation($"Connecting client {i}.");

            var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
            var connectTask = clientStream.ConnectAsync();

            clients.Add(new ClientStreamContext(clientStream, connectTask));

            try
            {
                // Attempt to connect for half a second. Assume we've hit the limit if this value is exceeded.
                await connectTask.WaitAsync(TimeSpan.FromSeconds(0.5));
                Logger.LogInformation($"Client {i} connect success.");
            }
            catch (TimeoutException)
            {
                Logger.LogInformation($"Client {i} connect timeout.");
                connectBlocked = true;
                break;
            }
        }

        Assert.True(connectBlocked, "Connect should be blocked before reaching the end of the connect loop.");

        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            Logger.LogInformation($"Accepting client {i} on the server.");
            var serverConnectionTask = connectionListener.AcceptAsync();

            await client.ConnectTask.DefaultTimeout();
            client.ServerConnection = await serverConnectionTask.DefaultTimeout();

            Logger.LogInformation($"Asserting client {i} is connected to the server.");
            Assert.True(client.ClientStream.IsConnected, "IsConnected should be true.");
            Assert.True(client.ConnectTask.IsCompletedSuccessfully, "ConnectTask should be completed.");
        }
    }

    private record ClientStreamContext(NamedPipeClientStream ClientStream, Task ConnectTask)
    {
        public ConnectionContext ServerConnection { get; set; }
    }

    [ConditionalFact]
    public async Task AcceptAsync_DisposeAfterCall_CleanExit()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAsync();

        await connectionListener.DisposeAsync().DefaultTimeout();

        // Assert
        Assert.Null(await acceptTask.AsTask().DefaultTimeout());

        Assert.DoesNotContain(LogMessages, m => m.LogLevel >= LogLevel.Error);
    }

    [ConditionalFact]
    public async Task BindAsync_ListenersSharePort_ThrowAddressInUse()
    {
        // Arrange
        await using var connectionListener1 = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);
        var pipeName = ((NamedPipeEndPoint)connectionListener1.EndPoint).PipeName;

        // Act & Assert
        await Assert.ThrowsAsync<AddressInUseException>(() => NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, pipeName: pipeName));
    }

    [ConditionalFact]
    public async Task BindAsync_ListenersSharePort_DisposeFirstListener_Success()
    {
        // Arrange
        var connectionListener1 = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);
        var pipeName = ((NamedPipeEndPoint)connectionListener1.EndPoint).PipeName;
        await connectionListener1.DisposeAsync();

        // Act & Assert
        await using var connectionListener2 = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, pipeName: pipeName);
        Assert.Equal(connectionListener1.EndPoint, connectionListener2.EndPoint);
    }
}

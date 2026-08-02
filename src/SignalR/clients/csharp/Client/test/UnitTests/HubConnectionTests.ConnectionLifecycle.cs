// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HubConnectionTests
{
    public class ConnectionLifecycle : VerifiableLoggedTest
    {
        // This tactic (using names and a dictionary) allows non-serializable data (like a Func) to be used in a theory AND get it to show in the new hierarchical view in Test Explorer as separate tests you can run individually.
        private static readonly IDictionary<string, Func<HubConnection, Task>> MethodsThatRequireActiveConnection = new Dictionary<string, Func<HubConnection, Task>>()
            {
                { nameof(HubConnection.InvokeCoreAsync), (connection) => connection.InvokeAsync("Foo") },
                { nameof(HubConnection.SendCoreAsync), (connection) => connection.SendAsync("Foo") },
                { nameof(HubConnection.StreamAsChannelCoreAsync), (connection) => connection.StreamAsChannelAsync<object>("Foo") },
            };

        public static IEnumerable<object[]> MethodsNamesThatRequireActiveConnection => MethodsThatRequireActiveConnection.Keys.Select(k => new object[] { k });

        [Fact]
        public async Task StartAsyncStartsTheUnderlyingConnection()
        {
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);
                Assert.Equal(HubConnectionState.Connected, connection.State);
            });
        }

        [Fact]
        public async Task StartAsyncThrowsIfPreviousStartIsAlreadyStarting()
        {
            // Set up StartAsync to wait on the syncPoint when starting
            var testConnection = new TestConnection(onStart: SyncPoint.Create(out var syncPoint));
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                var firstStart = connection.StartAsync();
                Assert.False(firstStart.IsCompleted);

                // Wait for us to be in IConnectionFactory.ConnectAsync
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                // Try starting again
                var secondStart = connection.StartAsync();
                Assert.False(secondStart.IsCompleted);

                // Release the sync point
                syncPoint.Continue();

                // The first start should finish fine, but the second throws an InvalidOperationException.
                await firstStart.DefaultTimeout();
                await Assert.ThrowsAsync<InvalidOperationException>(() => secondStart).DefaultTimeout();
            });
        }

        [Fact]
        public async Task StartingAfterStopCreatesANewConnection()
        {
            // Set up StartAsync to wait on the syncPoint when starting
            var createCount = 0;
            ValueTask<ConnectionContext> ConnectionFactory(EndPoint endPoint)
            {
                createCount += 1;
                return new TestConnection().StartAsync();
            }
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");
            var delegateConnectionFactory = new DelegateConnectionFactory(ConnectionFactory);
            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            await AsyncUsing(builder.Build(), async connection =>
            {
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.Equal(1, createCount);
                Assert.Equal(HubConnectionState.Connected, connection.State);

                await connection.StopAsync().DefaultTimeout();
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.Equal(2, createCount);
                Assert.Equal(HubConnectionState.Connected, connection.State);
            });
        }

        [Fact]
        public async Task StartingDuringStopCreatesANewConnection()
        {
            // Set up StartAsync to wait on the syncPoint when starting
            var createCount = 0;
            var onDisposeForFirstConnection = SyncPoint.Create(out var syncPoint);
            ValueTask<ConnectionContext> ConnectionFactory(EndPoint endPoint)
            {
                createCount += 1;
                return new TestConnection(onDispose: createCount == 1 ? onDisposeForFirstConnection : null).StartAsync();
            }

            var builder = new HubConnectionBuilder().WithUrl("http://example.com");
            var delegateConnectionFactory = new DelegateConnectionFactory(ConnectionFactory);
            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            await AsyncUsing(builder.Build(), async connection =>
            {
                await connection.StartAsync().DefaultTimeout();
                Assert.Equal(1, createCount);

                var stopTask = connection.StopAsync();

                // Wait to hit DisposeAsync on TestConnection (which should be after StopAsync has cleared the connection state)
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                // We should not yet be able to start now because StopAsync hasn't completed
                Assert.False(stopTask.IsCompleted);
                var startTask = connection.StartAsync();
                Assert.False(stopTask.IsCompleted);

                // When we release the sync point, the StopAsync task will finish
                syncPoint.Continue();
                await stopTask.DefaultTimeout();

                // Which will then allow StartAsync to finish.
                await startTask.DefaultTimeout();
            });
        }

        [Fact]
        public async Task StartAsyncWithFailedHandshakeCanBeStopped()
        {
            var handshakeConnectionErrorLogged = false;

            bool ExpectedErrors(WriteContext writeContext)
            {
                if (writeContext.LoggerName == typeof(HubConnection).FullName)
                {
                    if (writeContext.EventId.Name == "ErrorReceivingHandshakeResponse")
                    {
                        handshakeConnectionErrorLogged = true;
                        return true;
                    }

                    return writeContext.EventId.Name == "ErrorStartingConnection";
                }

                return false;
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var testConnection = new TestConnection(autoHandshake: false);
                await AsyncUsing(CreateHubConnection(testConnection, loggerFactory: LoggerFactory), async connection =>
                {
                    testConnection.Transport.Input.Complete();
                    try
                    {
                        await connection.StartAsync().DefaultTimeout();
                    }
                    catch
                    { }

                    await connection.StopAsync().DefaultTimeout();
                    Assert.True(testConnection.Started.IsCompleted);
                });
            }

            Assert.True(handshakeConnectionErrorLogged, "The connnection error during the handshake wasn't logged.");
        }

        [Theory]
        [MemberData(nameof(MethodsNamesThatRequireActiveConnection))]
        public async Task MethodsThatRequireStartedConnectionFailIfConnectionNotYetStarted(string name)
        {
            var method = MethodsThatRequireActiveConnection[name];

            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => method(connection)).DefaultTimeout();
                Assert.Equal($"The '{name}' method cannot be called if the connection is not active", ex.Message);
            });
        }

        [Theory]
        [MemberData(nameof(MethodsNamesThatRequireActiveConnection))]
        public async Task MethodsThatRequireStartedConnectionWaitForStartIfConnectionIsCurrentlyStarting(string name)
        {
            var method = MethodsThatRequireActiveConnection[name];

            // Set up StartAsync to wait on the syncPoint when starting
            var testConnection = new TestConnection(onStart: SyncPoint.Create(out var syncPoint));
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                // Start, and wait for the sync point to be hit
                var startTask = connection.StartAsync();
                Assert.False(startTask.IsCompleted);
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                // Run the method, but it will be waiting for the lock
                var targetTask = method(connection);

                // Release the SyncPoint
                syncPoint.Continue();

                // Wait for start to finish
                await startTask.DefaultTimeout();

                // We need some special logic to ensure InvokeAsync completes.
                if (string.Equals(name, nameof(HubConnection.InvokeCoreAsync)))
                {
                    await ForceLastInvocationToComplete(testConnection).DefaultTimeout();
                }

                // Wait for the method to complete.
                await targetTask.DefaultTimeout();
            });
        }

        [Fact]
        public async Task StatusIsNotConnectedUntilStartAsyncIsFinished()
        {
            // Set up StartAsync to wait on the syncPoint when starting
            var testConnection = new TestConnection(onStart: SyncPoint.Create(out var syncPoint));
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                // Start, and wait for the sync point to be hit
                var startTask = connection.StartAsync();
                Assert.False(startTask.IsCompleted);
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                Assert.Equal(HubConnectionState.Connecting, connection.State);

                // Release the SyncPoint
                syncPoint.Continue();

                // Wait for start to finish
                await startTask.DefaultTimeout();

                Assert.Equal(HubConnectionState.Connected, connection.State);
            });
        }

        [Fact]
        public async Task StatusIsDisconnectedInCloseEvent()
        {
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                var closed = new TaskCompletionSource();
                connection.Closed += exception =>
                {
                    closed.TrySetResult();
                    Assert.Equal(HubConnectionState.Disconnected, connection.State);
                    return Task.CompletedTask;
                };

                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);
                Assert.Equal(HubConnectionState.Connected, connection.State);

                await connection.StopAsync().DefaultTimeout();
                await testConnection.Disposed.DefaultTimeout();
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await closed.Task.DefaultTimeout();
            });
        }

        [Fact]
        public async Task StopAsyncStopsConnection()
        {
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);

                await connection.StopAsync().DefaultTimeout();
                await testConnection.Disposed.DefaultTimeout();
            });
        }

        [Fact]
        public async Task StopAsyncNoOpsIfConnectionNotYetStarted()
        {
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                await connection.StopAsync().DefaultTimeout();
                Assert.False(testConnection.Disposed.IsCompleted);
            });
        }

        [Fact]
        public async Task StopAsyncNoOpsIfConnectionAlreadyStopped()
        {
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);
                Assert.Equal(HubConnectionState.Connected, connection.State);

                await connection.StopAsync().DefaultTimeout();
                await testConnection.Disposed.DefaultTimeout();
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StopAsync().DefaultTimeout();
                Assert.Equal(HubConnectionState.Disconnected, connection.State);
            });
        }

        [Fact]
        public async Task StopAsyncOnInactiveConnectionDoesNotAffectNextStartAsync()
        {
            // Regression test:
            // If there wasn't an active underlying connection, StopAsync would leave a CTS canceled which would cause the next StartAsync to fail
            var testConnection = new TestConnection();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StopAsync().DefaultTimeout();
                Assert.False(testConnection.Disposed.IsCompleted);
                Assert.Equal(HubConnectionState.Disconnected, connection.State);

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);
                Assert.Equal(HubConnectionState.Connected, connection.State);
            });
        }

        [Fact]
        public async Task CompletingTheTransportSideMarksConnectionAsClosed()
        {
            var testConnection = new TestConnection();
            var closed = new TaskCompletionSource();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                connection.Closed += (e) =>
                {
                    closed.TrySetResult();
                    return Task.CompletedTask;
                };
                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);

                // Complete the transport side and wait for the connection to close
                testConnection.CompleteFromTransport();
                await closed.Task.DefaultTimeout();

                // We should be stopped now
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync("Foo")).DefaultTimeout();
                Assert.Equal($"The '{nameof(HubConnection.SendCoreAsync)}' method cannot be called if the connection is not active", ex.Message);
            });
        }

        [Fact]
        public async Task TransportCompletionWhileShuttingDownIsNoOp()
        {
            var testConnection = new TestConnection();
            var connectionClosed = new TaskCompletionSource();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                connection.Closed += (e) =>
                {
                    connectionClosed.TrySetResult();
                    return Task.CompletedTask;
                };

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);

                // Start shutting down and complete the transport side
                var stopTask = connection.StopAsync();
                testConnection.CompleteFromTransport();

                // Wait for the connection to close.
                await testConnection.Transport.Input.CompleteAsync();

                // The stop should be completed.
                await stopTask.DefaultTimeout();

                // The HubConnection should now be closed.
                await connectionClosed.Task.DefaultTimeout();

                // We should be stopped now
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync("Foo")).DefaultTimeout();
                Assert.Equal($"The '{nameof(HubConnection.SendCoreAsync)}' method cannot be called if the connection is not active", ex.Message);

                await testConnection.Disposed.DefaultTimeout();

                Assert.Equal(1, testConnection.DisposeCount);
            });
        }

        [Fact]
        public async Task StopAsyncDuringUnderlyingConnectionCloseWaitsAndNoOps()
        {
            var testConnection = new TestConnection();
            var connectionClosed = new TaskCompletionSource();
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                connection.Closed += (e) =>
                {
                    connectionClosed.TrySetResult();
                    return Task.CompletedTask;
                };

                await connection.StartAsync().DefaultTimeout();
                Assert.True(testConnection.Started.IsCompleted);

                // Complete the transport side and wait for the connection to close
                testConnection.CompleteFromTransport();

                // Start stopping manually (these can't be synchronized by a Sync Point because the transport is disposed outside the lock)
                var stopTask = connection.StopAsync();

                await testConnection.Disposed.DefaultTimeout();

                // Wait for the stop task to complete and the closed event to fire
                await stopTask.DefaultTimeout();
                await connectionClosed.Task.DefaultTimeout();

                // We should be stopped now
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync("Foo")).DefaultTimeout();
                Assert.Equal($"The '{nameof(HubConnection.SendCoreAsync)}' method cannot be called if the connection is not active", ex.Message);
            });
        }

        [Theory]
        [MemberData(nameof(MethodsNamesThatRequireActiveConnection))]
        public async Task MethodsThatRequireActiveConnectionWaitForStopAndFailIfConnectionIsCurrentlyStopping(string methodName)
        {
            var method = MethodsThatRequireActiveConnection[methodName];

            // Set up StartAsync to wait on the syncPoint when starting
            var testConnection = new TestConnection(onDispose: SyncPoint.Create(out var syncPoint));
            await AsyncUsing(CreateHubConnection(testConnection), async connection =>
            {
                await connection.StartAsync().DefaultTimeout();

                // Stop and invoke the method. These two aren't synchronizable via a Sync Point any more because the transport is disposed
                // outside the lock :(
                var disposeTask = connection.StopAsync();

                // Wait to hit DisposeAsync on TestConnection (which should be after StopAsync has cleared the connection state)
                await syncPoint.WaitForSyncPoint().DefaultTimeout();

                var targetTask = method(connection);

                // Release the sync point
                syncPoint.Continue();

                // Wait for the method to complete, with an expected error.
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => targetTask).DefaultTimeout();
                Assert.Equal($"The '{methodName}' method cannot be called if the connection is not active", ex.Message);

                await disposeTask.DefaultTimeout();
            });
        }

        [Fact]
        public async Task ClientTimesoutWhenHandshakeResponseTakesTooLong()
        {
            var handshakeTimeoutLogged = false;

            bool ExpectedErrors(WriteContext writeContext)
            {
                if (writeContext.LoggerName == typeof(HubConnection).FullName)
                {
                    if (writeContext.EventId.Name == "ErrorHandshakeTimedOut")
                    {
                        handshakeTimeoutLogged = true;
                        return true;
                    }

                    return writeContext.EventId.Name == "ErrorStartingConnection";
                }

                return false;
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var connection = new TestConnection(autoHandshake: false);
                var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
                try
                {
                    hubConnection.HandshakeTimeout = TimeSpan.FromMilliseconds(1);

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hubConnection.StartAsync()).DefaultTimeout();
                    Assert.Equal(HubConnectionState.Disconnected, hubConnection.State);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }

            Assert.True(handshakeTimeoutLogged, "The handshake timeout wasn't logged.");
        }

        [Fact]
        public async Task StartAsyncWithTriggeredCancellationTokenIsCanceled()
        {
            using (StartVerifiableLog())
            {
                var onStartCalled = false;
                var connection = new TestConnection(onStart: () =>
                {
                    onStartCalled = true;
                    return Task.CompletedTask;
                });
                var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
                try
                {
                    await Assert.ThrowsAsync<TaskCanceledException>(() => hubConnection.StartAsync(new CancellationToken(canceled: true))).DefaultTimeout();
                    Assert.False(onStartCalled);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartAsyncCanTriggerCancellationTokenToCancelHandshake()
        {
            var handshakeCancellationLogged = false;

            bool ExpectedErrors(WriteContext writeContext)
            {
                if (writeContext.LoggerName == typeof(HubConnection).FullName)
                {
                    if (writeContext.EventId.Name == "ErrorHandshakeCanceled")
                    {
                        handshakeCancellationLogged = true;
                        return true;
                    }

                    return writeContext.EventId.Name == "ErrorStartingConnection";
                }

                return false;
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var cts = new CancellationTokenSource();
                TestConnection connection = null;

                connection = new TestConnection(autoHandshake: false);

                var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
                // We want to make sure the cancellation is because of the token passed to StartAsync
                hubConnection.HandshakeTimeout = Timeout.InfiniteTimeSpan;

                try
                {
                    var startTask = hubConnection.StartAsync(cts.Token);

                    await connection.ReadSentTextMessageAsync().DefaultTimeout();
                    cts.Cancel();

                    // We aren't worried about the exact message and it's localized so asserting it is non-trivial.
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask).DefaultTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }

            Assert.True(handshakeCancellationLogged, "The handshake cancellation wasn't logged.");
        }

        [Fact]
        public async Task HubConnectionClosesWithErrorIfTerminatedWithPartialMessage()
        {
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");
            var innerConnection = new TestConnection();

            var delegateConnectionFactory = new DelegateConnectionFactory(
                endPoint => innerConnection.StartAsync());
            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            var hubConnection = builder.Build();
            var closedEventTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += e =>
            {
                closedEventTcs.SetResult(e);
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync().DefaultTimeout();

            await innerConnection.Application.Output.WriteAsync(Encoding.UTF8.GetBytes(new[] { '{' })).DefaultTimeout();
            innerConnection.Application.Output.Complete();

            var exception = await closedEventTcs.Task.DefaultTimeout();
            Assert.Equal("Connection terminated while reading a message.", exception.Message);
        }

        private static async Task ForceLastInvocationToComplete(TestConnection testConnection)
        {
            // We need to "complete" the invocation
            var message = await testConnection.ReadSentTextMessageAsync();
            var json = JObject.Parse(message); // Gotta remove the record separator.
            await testConnection.ReceiveJsonMessage(new
            {
                type = HubProtocolConstants.CompletionMessageType,
                invocationId = json["invocationId"],
            });
        }

        // A helper that we wouldn't want to use in product code, but is fine for testing until IAsyncDisposable arrives :)
        private static async Task AsyncUsing(HubConnection connection, Func<HubConnection, Task> action)
        {
            try
            {
                // Using OrTimeout here will hide any timeout issues in the test :(.
                await action(connection);
            }
            finally
            {
                // Dispose isn't under test here, so fire and forget so that errors/timeouts here don't cause
                // test errors that mask the real errors.
                _ = connection.DisposeAsync();
            }
        }
    }
}

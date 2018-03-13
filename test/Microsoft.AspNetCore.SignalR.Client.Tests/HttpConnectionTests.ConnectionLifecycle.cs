// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class ConnectionLifecycle : LoggedTest
        {
            public ConnectionLifecycle(ITestOutputHelper output) : base(output)
            {
            }

            [Fact]
            public async Task CannotStartRunningConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(CreateConnection(loggerFactory: loggerFactory), async (connection, closed) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        var exception =
                            await Assert.ThrowsAsync<InvalidOperationException>(
                                async () => await connection.StartAsync(TransferFormat.Text).OrTimeout());
                        Assert.Equal("Cannot start a connection that is not in the Disconnected state.", exception.Message);
                    });
                }
            }


            [Fact]
            public async Task CannotStartConnectionDisposedAfterStarting()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: loggerFactory),
                        async (connection, closed) =>
                        {
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            await connection.DisposeAsync();
                            var exception =
                                await Assert.ThrowsAsync<InvalidOperationException>(
                                    async () => await connection.StartAsync(TransferFormat.Text).OrTimeout());

                            Assert.Equal("Cannot start a connection that is not in the Disconnected state.", exception.Message);
                        });
                }
            }

            [Fact]
            public async Task CannotStartDisposedConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: loggerFactory),
                        async (connection, closed) =>
                        {
                            await connection.DisposeAsync();
                            var exception =
                                await Assert.ThrowsAsync<InvalidOperationException>(
                                    async () => await connection.StartAsync(TransferFormat.Text).OrTimeout());

                            Assert.Equal("Cannot start a connection that is not in the Disconnected state.", exception.Message);
                        });
                }
            }

            [Fact]
            public async Task CanDisposeStartingConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: loggerFactory,
                            transport: new TestTransport(
                                onTransportStart: SyncPoint.Create(out var transportStart),
                                onTransportStop: SyncPoint.Create(out var transportStop))),
                        async (connection, closed) =>
                    {
                        // Start the connection and wait for the transport to start up.
                        var startTask = connection.StartAsync(TransferFormat.Text);
                        await transportStart.WaitForSyncPoint().OrTimeout();

                        // While the transport is starting, dispose the connection
                        var disposeTask = connection.DisposeAsync();
                        transportStart.Continue(); // We need to release StartAsync, because Dispose waits for it.

                        // Wait for start to finish, as that has to finish before the transport will be stopped.
                        await startTask.OrTimeout();

                        // Then release DisposeAsync (via the transport StopAsync call)
                        await transportStop.WaitForSyncPoint().OrTimeout();
                        transportStop.Continue();
                    });
                }
            }

            [Theory]
            [InlineData(2)]
            [InlineData(3)]
            public async Task TransportThatFailsToStartOnceFallsBack(int passThreshold)
            {
                using (StartLog(out var loggerFactory))
                {
                    var startCounter = 0;
                    var expected = new Exception("Transport failed to start");

                    Task OnTransportStart()
                    {
                        startCounter++;
                        if (startCounter < passThreshold)
                        {
                            // Succeed next time
                            return Task.FromException(expected);
                        }
                        else
                        {
                            return Task.CompletedTask;
                        }
                    }

                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: loggerFactory,
                            transport: new TestTransport(onTransportStart: OnTransportStart)),
                        async (connection, closed) =>
                    {
                        Assert.Equal(0, startCounter);
                        await connection.StartAsync(TransferFormat.Text);
                        Assert.Equal(passThreshold, startCounter);
                    });
                }
            }

            [Fact]
            public async Task StartThrowsAfterAllTransportsFail()
            {
                using (StartLog(out var loggerFactory))
                {
                    var startCounter = 0;
                    var expected = new Exception("Transport failed to start");
                    Task OnTransportStart()
                    {
                        startCounter++;
                        return Task.FromException(expected);
                    }

                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: loggerFactory,
                            transport: new TestTransport(onTransportStart: OnTransportStart)),
                        async (connection, closed) =>
                        {
                            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync(TransferFormat.Text));
                            Assert.Equal("Unable to connect to the server with any of the available transports.", ex.Message);
                            Assert.Equal(3, startCounter);
                        });
                }
            }

            [Fact]
            public async Task CanStartStoppedConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: loggerFactory),
                        async (connection, closed) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.StopAsync().OrTimeout();
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                    });
                }
            }

            [Fact]
            public async Task CanStopStartingConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: loggerFactory,
                            transport: new TestTransport(onTransportStart: SyncPoint.Create(out var transportStart))),
                        async (connection, closed) =>
                    {
                        // Start and wait for the transport to start up.
                        var startTask = connection.StartAsync(TransferFormat.Text);
                        await transportStart.WaitForSyncPoint().OrTimeout();

                        // Stop the connection while it's starting
                        var stopTask = connection.StopAsync();
                        transportStart.Continue(); // We need to release Start in order for Stop to begin working.

                        // Wait for start to finish, which will allow stop to finish and the connection to close.
                        await startTask.OrTimeout();
                        await stopTask.OrTimeout();
                        await closed.OrTimeout();
                    });
                }
            }

            [Fact]
            public async Task StoppingStoppingConnectionNoOps()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: loggerFactory),
                        async (connection, closed) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await Task.WhenAll(connection.StopAsync(), connection.StopAsync()).OrTimeout();
                        await closed.OrTimeout();
                    });
                }
            }

            [Fact]
            public async Task CanStartConnectionAfterConnectionStoppedWithError()
            {
                using (StartLog(out var loggerFactory))
                {
                    var httpHandler = new TestHttpMessageHandler();

                    var longPollResult = new TaskCompletionSource<HttpResponseMessage>();
                    httpHandler.OnLongPoll(cancellationToken =>
                    {
                        cancellationToken.Register(() =>
                        {
                            longPollResult.TrySetResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                        });
                        return longPollResult.Task;
                    });

                    httpHandler.OnSocketSend((data, _) =>
                    {
                        Assert.Collection(data, i => Assert.Equal(0x42, i));
                        return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError));
                    });

                    await WithConnectionAsync(
                        CreateConnection(httpHandler, loggerFactory),
                        async (connection, closed) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.SendAsync(new byte[] { 0x42 }).OrTimeout();

                        // Wait for the connection to close, because the send failed.
                        await Assert.ThrowsAsync<HttpRequestException>(() => closed.OrTimeout());

                        // Start it up again
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                    });
                }
            }

            [Fact]
            public async Task DisposedStoppingConnectionDisposesConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: loggerFactory,
                            transport: new TestTransport(onTransportStop: SyncPoint.Create(out var transportStop))),
                        async (connection, closed) =>
                    {
                        // Start the connection
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();

                        // Stop the connection
                        var stopTask = connection.StopAsync().OrTimeout();

                        // Once the transport starts shutting down
                        await transportStop.WaitForSyncPoint();

                        // Start disposing and allow it to finish shutting down
                        var disposeTask = connection.DisposeAsync().OrTimeout();
                        transportStop.Continue();

                        // Wait for the tasks to complete
                        await stopTask.OrTimeout();
                        await closed.OrTimeout();
                        await disposeTask.OrTimeout();

                        // We should be disposed and thus unable to restart.
                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync(TransferFormat.Text).OrTimeout());
                        Assert.Equal("Cannot start a connection that is not in the Disconnected state.", exception.Message);
                    });
                }
            }

            [Fact]
            public async Task CanDisposeStoppedConnection()
            {
                using (StartLog(out var loggerFactory))
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: loggerFactory),
                        async (connection, closed) =>
                        {
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            await connection.StopAsync().OrTimeout();
                            await closed.OrTimeout();
                            await connection.DisposeAsync().OrTimeout();
                        });
                }
            }

            [Fact]
            public Task ClosedEventRaisedWhenTheClientIsDisposed()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.DisposeAsync().OrTimeout();
                        await closed.OrTimeout();
                    });
            }

            [Fact]
            public async Task ConnectionClosedWhenTransportFails()
            {
                var testTransport = new TestTransport();

                var expected = new Exception("Whoops!");

                await WithConnectionAsync(
                    CreateConnection(transport: testTransport),
                async (connection, closed) =>
                {
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();
                    testTransport.Application.Output.Complete(expected);
                    var actual = await Assert.ThrowsAsync<Exception>(() => closed.OrTimeout());
                    Assert.Same(expected, actual);

                    var sendException = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync(new byte[0]).OrTimeout());
                    Assert.Equal("Cannot send messages when the connection is not in the Connected state.", sendException.Message);
                });
            }

            [Fact]
            public Task ClosedEventNotRaisedWhenTheClientIsStoppedButWasNeverStarted()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection, closed) =>
                {
                    await connection.DisposeAsync().OrTimeout();
                    Assert.False(closed.IsCompleted);
                });
            }

            [Fact]
            public async Task TransportIsStoppedWhenConnectionIsStopped()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                // Just keep returning data when polled
                testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.OK));

                using (var httpClient = new HttpClient(testHttpHandler))
                {
                    var longPollingTransport = new LongPollingTransport(httpClient);
                    await WithConnectionAsync(
                        CreateConnection(transport: longPollingTransport),
                        async (connection, closed) =>
                        {
                            // Start the transport
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            Assert.False(longPollingTransport.Running.IsCompleted, "Expected that the transport would still be running");

                            // Stop the connection, and we should stop the transport
                            await connection.StopAsync().OrTimeout();
                            await longPollingTransport.Running.OrTimeout();
                        });
                }
            }
        }
    }
}

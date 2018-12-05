// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class ConnectionLifecycle : VerifiableLoggedTest
        {
            [Fact]
            public async Task CanStartStartedConnection()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(CreateConnection(loggerFactory: LoggerFactory), async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                    });
                }
            }

            [Fact]
            public async Task CanStartStartingConnection()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: LoggerFactory, transport: new TestTransport(onTransportStart: SyncPoint.Create(out var syncPoint))),
                        async (connection) =>
                        {
                            var firstStart = connection.StartAsync(TransferFormat.Text).OrTimeout();
                            await syncPoint.WaitForSyncPoint();
                            var secondStart = connection.StartAsync(TransferFormat.Text).OrTimeout();
                            syncPoint.Continue();

                            await firstStart;
                            await secondStart;
                        });
                }
            }

            [Fact]
            public async Task CannotStartConnectionOnceDisposed()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: LoggerFactory),
                        async (connection) =>
                        {
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            await connection.DisposeAsync();
                            var exception =
                                await Assert.ThrowsAsync<ObjectDisposedException>(
                                    async () => await connection.StartAsync(TransferFormat.Text).OrTimeout());

                            Assert.Equal(nameof(HttpConnection), exception.ObjectName);
                        });
                }
            }

            [Theory]
            [InlineData(2)]
            [InlineData(3)]
            public async Task TransportThatFailsToStartFallsBack(int passThreshold)
            {
                bool ExpectedErrors(WriteContext writeContext)
                {
                    return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                           writeContext.EventId.Name == "ErrorStartingTransport";
                }

                using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
                {
                    var startCounter = 0;
                    var expected = new Exception("Transport failed to start");

                    // We have 4 cases here. Falling back once, falling back twice and each of these 
                    // with WebSockets available and not. If Websockets aren't available and 
                    // we can't to test the fallback once scenario we don't decrement the passthreshold
                    // because we still try to start twice (SSE and LP).
                    if (!TestHelpers.IsWebSocketsSupported() && passThreshold > 2)
                    {
                        passThreshold -= 1;
                    }

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
                            loggerFactory: LoggerFactory,
                            transportType: HttpTransports.All,
                            transport: new TestTransport(onTransportStart: OnTransportStart)),
                        async (connection) =>
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
                bool ExpectedErrors(WriteContext writeContext)
                {
                    return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                           writeContext.EventId.Name == "ErrorStartingTransport";
                }

                using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
                {
                    var startCounter = 0;
                    var availableTransports = 3;
                    var expected = new Exception("Transport failed to start");
                    Task OnTransportStart()
                    {
                        startCounter++;
                        return Task.FromException(expected);
                    }

                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: LoggerFactory,
                            transportType: HttpTransports.All,
                            transport: new TestTransport(onTransportStart: OnTransportStart)),
                        async (connection) =>
                        {
                            var ex = await Assert.ThrowsAsync<AggregateException>(() => connection.StartAsync(TransferFormat.Text));
                            Assert.Equal("Unable to connect to the server with any of the available transports. " +
                                "(WebSockets failed: Transport failed to start) (ServerSentEvents failed: Transport failed to start) (LongPolling failed: Transport failed to start)",
                                ex.Message);

                            // If websockets aren't supported then we expect one less attmept to start.
                            if (!TestHelpers.IsWebSocketsSupported())
                            {
                                availableTransports -= 1;
                            }

                            Assert.Equal(availableTransports, startCounter);
                        });
                }
            }

            [Fact]
            public async Task CanDisposeUnstartedConnection()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(
                        CreateConnection(loggerFactory: LoggerFactory),
                        async (connection) =>
                        {
                            await connection.DisposeAsync();

                        });
                }
            }

            [Fact]
            public async Task CanDisposeStartingConnection()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: LoggerFactory,
                            transport: new TestTransport(
                                onTransportStart: SyncPoint.Create(out var transportStart),
                                onTransportStop: SyncPoint.Create(out var transportStop))),
                        async (connection) =>
                        {
                            // Start the connection and wait for the transport to start up.
                            var startTask = connection.StartAsync(TransferFormat.Text);
                            await transportStart.WaitForSyncPoint().OrTimeout();

                            // While the transport is starting, dispose the connection
                            var disposeTask = connection.DisposeAsync().OrTimeout();
                            transportStart.Continue(); // We need to release StartAsync, because Dispose waits for it.

                            // Wait for start to finish, as that has to finish before the transport will be stopped.
                            await startTask.OrTimeout();

                            // Then release DisposeAsync (via the transport StopAsync call)
                            await transportStop.WaitForSyncPoint().OrTimeout();
                            transportStop.Continue();

                            // Dispose should finish
                            await disposeTask;
                        });
                }
            }

            [Fact]
            public async Task CanDisposeDisposingConnection()
            {
                using (StartVerifiableLog())
                {
                    await WithConnectionAsync(
                        CreateConnection(
                            loggerFactory: LoggerFactory,
                            transport: new TestTransport(onTransportStop: SyncPoint.Create(out var transportStop))),
                        async (connection) =>
                    {
                        // Start the connection
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();

                        // Dispose the connection
                        var stopTask = connection.DisposeAsync().OrTimeout();

                        // Once the transport starts shutting down
                        await transportStop.WaitForSyncPoint();
                        Assert.False(stopTask.IsCompleted);

                        // Start disposing again, and then let the first dispose continue
                        var disposeTask = connection.DisposeAsync().OrTimeout();
                        transportStop.Continue();

                        // Wait for the tasks to complete
                        await stopTask.OrTimeout();
                        await disposeTask.OrTimeout();

                        // We should be disposed and thus unable to restart.
                        await AssertDisposedAsync(connection);
                    });
                }
            }

            [Fact]
            public async Task TransportIsStoppedWhenConnectionIsDisposed()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                using (var httpClient = new HttpClient(testHttpHandler))
                {
                    var testTransport = new TestTransport();
                    await WithConnectionAsync(
                        CreateConnection(transport: testTransport),
                        async (connection) =>
                        {
                            // Start the transport
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            Assert.NotNull(testTransport.Receiving);
                            Assert.False(testTransport.Receiving.IsCompleted);

                            // Stop the connection, and we should stop the transport
                            await connection.DisposeAsync().OrTimeout();
                            await testTransport.Receiving.OrTimeout();
                        });
                }
            }

            [Fact]
            public async Task TransportPipeIsCompletedWhenErrorOccursInTransport()
            {
                bool ExpectedErrors(WriteContext writeContext)
                {
                    return writeContext.LoggerName == typeof(LongPollingTransport).FullName &&
                           writeContext.EventId.Name == "ErrorSending";
                }

                using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
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
                    httpHandler.OnLongPollDelete(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));

                    httpHandler.OnSocketSend((data, _) =>
                    {
                        Assert.Collection(data, i => Assert.Equal(0x42, i));
                        return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError));
                    });

                    await WithConnectionAsync(
                        CreateConnection(httpHandler, LoggerFactory),
                        async (connection) =>
                        {
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                            await connection.Transport.Output.WriteAsync(new byte[] { 0x42 }).OrTimeout();

                            // We should get the exception in the transport input completion.
                            await Assert.ThrowsAsync<HttpRequestException>(() => connection.Transport.Input.WaitForWriterToComplete());
                        });
                }
            }

            [Fact]
            public async Task SSEWontStartIfSuccessfulConnectionIsNotEstablished()
            {
                bool ExpectedErrors(WriteContext writeContext)
                {
                    return writeContext.LoggerName == typeof(HttpConnection).FullName &&
                           writeContext.EventId.Name == "ErrorStartingTransport";
                }

                using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
                {
                    var httpHandler = new TestHttpMessageHandler();

                    httpHandler.OnGet("/?id=00000000-0000-0000-0000-000000000000", (_, __) =>
                    {
                        return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError));
                    });

                    var sse = new ServerSentEventsTransport(new HttpClient(httpHandler), LoggerFactory);

                    await WithConnectionAsync(
                        CreateConnection(httpHandler, loggerFactory: LoggerFactory, transport: sse),
                        async (connection) =>
                        {
                            await Assert.ThrowsAsync<AggregateException>(
                                () => connection.StartAsync(TransferFormat.Text).OrTimeout());
                        });
                }
            }

            [Fact]
            public async Task SSEWaitsForResponseToStart()
            {
                using (StartVerifiableLog())
                {
                    var httpHandler = new TestHttpMessageHandler();

                    var connectResponseTcs = new TaskCompletionSource<object>();
                    httpHandler.OnGet("/?id=00000000-0000-0000-0000-000000000000", async (_, __) =>
                    {
                        await connectResponseTcs.Task;
                        return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                    });

                    var sse = new ServerSentEventsTransport(new HttpClient(httpHandler), LoggerFactory);

                    await WithConnectionAsync(
                        CreateConnection(httpHandler, loggerFactory: LoggerFactory, transport: sse),
                        async (connection) =>
                        {
                            var startTask = connection.StartAsync(TransferFormat.Text).OrTimeout();
                            Assert.False(connectResponseTcs.Task.IsCompleted);
                            Assert.False(startTask.IsCompleted);
                            connectResponseTcs.TrySetResult(null);
                            await startTask;
                        });
                }
            }

            private static async Task AssertDisposedAsync(HttpConnection connection)
            {
                var exception =
                    await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.StartAsync(TransferFormat.Text).OrTimeout());
                Assert.Equal(nameof(HttpConnection), exception.ObjectName);
            }
        }
    }
}

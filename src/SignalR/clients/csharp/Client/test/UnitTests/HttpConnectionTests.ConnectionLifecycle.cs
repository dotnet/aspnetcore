// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

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
                    await connection.StartAsync().DefaultTimeout();
                    await connection.StartAsync().DefaultTimeout();
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
                        var firstStart = connection.StartAsync();
                        await syncPoint.WaitForSyncPoint().DefaultTimeout();
                        var secondStart = connection.StartAsync();
                        syncPoint.Continue();

                        await firstStart.DefaultTimeout();
                        await secondStart.DefaultTimeout();
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
                        await connection.StartAsync().DefaultTimeout();
                        await connection.DisposeAsync().DefaultTimeout();
                        var exception =
                            await Assert.ThrowsAsync<ObjectDisposedException>(
                                async () => await connection.StartAsync()).DefaultTimeout();

                        Assert.Equal(typeof(HttpConnection).FullName, exception.ObjectName);
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
                    await connection.StartAsync().DefaultTimeout();
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
                        var ex = await Assert.ThrowsAsync<AggregateException>(() => connection.StartAsync()).DefaultTimeout();
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
                        await connection.DisposeAsync().DefaultTimeout();
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
                        var startTask = connection.StartAsync();
                        await transportStart.WaitForSyncPoint().DefaultTimeout();

                        // While the transport is starting, dispose the connection
                        var disposeTask = connection.DisposeAsync();
                        transportStart.Continue(); // We need to release StartAsync, because Dispose waits for it.

                        // Wait for start to finish, as that has to finish before the transport will be stopped.
                        await startTask.DefaultTimeout();

                        // Then release DisposeAsync (via the transport StopAsync call)
                        await transportStop.WaitForSyncPoint().DefaultTimeout();
                        transportStop.Continue();

                        // Dispose should finish
                        await disposeTask.DefaultTimeout();
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
                    await connection.StartAsync().DefaultTimeout();

                    // Dispose the connection
                    var stopTask = connection.DisposeAsync();

                    // Once the transport starts shutting down
                    await transportStop.WaitForSyncPoint().DefaultTimeout();
                    Assert.False(stopTask.IsCompleted);

                    // Start disposing again, and then let the first dispose continue
                    var disposeTask = connection.DisposeAsync();
                    transportStop.Continue();

                    // Wait for the tasks to complete
                    await stopTask.DefaultTimeout();
                    await disposeTask.DefaultTimeout();

                    // We should be disposed and thus unable to restart.
                    await AssertDisposedAsync(connection).DefaultTimeout();
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
                        await connection.StartAsync().DefaultTimeout();
                        Assert.NotNull(testTransport.Receiving);
                        Assert.False(testTransport.Receiving.IsCompleted);

                        // Stop the connection, and we should stop the transport
                        await connection.DisposeAsync().DefaultTimeout();
                        await testTransport.Receiving.DefaultTimeout();
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
                        await connection.StartAsync().DefaultTimeout();
                        await connection.Transport.Output.WriteAsync(new byte[] { 0x42 }).DefaultTimeout();

                        await Assert.ThrowsAsync<HttpRequestException>(async () => await connection.Transport.Input.ReadAsync());
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

                var sse = new ServerSentEventsTransport(new HttpClient(httpHandler), loggerFactory: LoggerFactory);

                await WithConnectionAsync(
                    CreateConnection(httpHandler, loggerFactory: LoggerFactory, transport: sse),
                    async (connection) =>
                    {
                        await Assert.ThrowsAsync<AggregateException>(
                            () => connection.StartAsync().DefaultTimeout());
                    });
            }
        }

        [Fact]
        public async Task SSEWaitsForResponseToStart()
        {
            using (StartVerifiableLog())
            {
                var httpHandler = new TestHttpMessageHandler();

                var connectResponseTcs = new TaskCompletionSource();
                httpHandler.OnGet("/?id=00000000-0000-0000-0000-000000000000", async (_, __) =>
                {
                    await connectResponseTcs.Task;
                    return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                });

                var sse = new ServerSentEventsTransport(new HttpClient(httpHandler), loggerFactory: LoggerFactory);

                await WithConnectionAsync(
                    CreateConnection(httpHandler, loggerFactory: LoggerFactory, transport: sse),
                    async (connection) =>
                    {
                        var startTask = connection.StartAsync();
                        Assert.False(connectResponseTcs.Task.IsCompleted);
                        Assert.False(startTask.IsCompleted);
                        connectResponseTcs.TrySetResult();
                        await startTask.DefaultTimeout();
                    });
            }
        }

        [Fact]
        public async Task CanCancelStartingConnectionAfterNegotiate()
        {
            using (StartVerifiableLog())
            {
                // Set up a SyncPoint within Negotiate, so we can verify
                // that the call has gotten that far
                var negotiateSyncPoint = new SyncPoint();
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
                testHttpHandler.OnNegotiate(async (request, cancellationToken) =>
                {
                    // Wait here for the test code to cancel the "outer" token
                    await negotiateSyncPoint.WaitToContinue().DefaultTimeout();

                    // Cancel
                    cancellationToken.ThrowIfCancellationRequested();

                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection) =>
                    {
                        // Kick off StartAsync, but don't wait for it
                        var cts = new CancellationTokenSource();
                        var startTask = connection.StartAsync(cts.Token);

                        // Wait for the connection to get to the "WaitToContinue" call above,
                        // which means it has gotten to Negotiate
                        await negotiateSyncPoint.WaitForSyncPoint().DefaultTimeout();

                        // Assert that StartAsync has not yet been canceled
                        Assert.False(startTask.IsCanceled);

                        // Cancel StartAsync, then "release" the SyncPoint
                        // so the negotiate handler can keep going
                        cts.Cancel();
                        negotiateSyncPoint.Continue();

                        // Assert that StartAsync was canceled
                        await Assert.ThrowsAsync<TaskCanceledException>(() => startTask).DefaultTimeout();
                    });
            }
        }

        [Fact]
        public async Task CancellationTokenFromStartPassedToTransport()
        {
            using (StartVerifiableLog())
            {
                var cts = new CancellationTokenSource();
                var httpHandler = new TestHttpMessageHandler();

                await WithConnectionAsync(
                    CreateConnection(httpHandler,
                    transport: new TestTransport(onTransportStart: () =>
                    {
                        // Cancel the token when the transport is starting  which will fail the startTask.
                        cts.Cancel();
                        return Task.CompletedTask;
                    })),
                    async (connection) =>
                    {
                        // We aggregate failures that happen when we start the transport. The operation canceled exception will
                        // be an inner exception.
                        var ex = await Assert.ThrowsAsync<AggregateException>(async () => await connection.StartAsync(cts.Token)).DefaultTimeout();
                        Assert.Equal(3, ex.InnerExceptions.Count);
                        var innerEx = ex.InnerExceptions[2];
                        var innerInnerEx = innerEx.InnerException;
                        Assert.IsType<OperationCanceledException>(innerInnerEx);
                    });
            }
        }

        [Fact]
        public async Task CanceledCancellationTokenPassedToStartThrows()
        {
            using (StartVerifiableLog())
            {
                bool transportStartCalled = false;
                var httpHandler = new TestHttpMessageHandler();

                await WithConnectionAsync(
                    CreateConnection(httpHandler,
                    transport: new TestTransport(onTransportStart: () =>
                    {
                        transportStartCalled = true;
                        return Task.CompletedTask;
                    })),
                    async (connection) =>
                    {
                        await Assert.ThrowsAsync<TaskCanceledException>(async () => await connection.StartAsync(new CancellationToken(canceled: true))).DefaultTimeout();
                    });

                Assert.False(transportStartCalled);
            }
        }

        [Fact]
        public async Task SSECanBeCanceled()
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
                    // Simulating a cancellationToken canceling this request.
                    throw new OperationCanceledException("Cancel SSE Start.");
                });

                var sse = new ServerSentEventsTransport(new HttpClient(httpHandler), loggerFactory: LoggerFactory);

                await WithConnectionAsync(
                    CreateConnection(httpHandler, loggerFactory: LoggerFactory, transport: sse, transportType: HttpTransportType.ServerSentEvents),
                    async (connection) =>
                    {
                        var ex = await Assert.ThrowsAsync<AggregateException>(async () => await connection.StartAsync()).DefaultTimeout();
                    });
            }
        }

        [Fact]
        public async Task LongPollingTransportCanBeCanceled()
        {
            using (StartVerifiableLog())
            {
                var cts = new CancellationTokenSource();

                var httpHandler = new TestHttpMessageHandler(autoNegotiate: false);
                httpHandler.OnNegotiate((request, cancellationToken) =>
                {
                    // Cancel token so that the first request poll will throw
                    cts.Cancel();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
                });

                var lp = new LongPollingTransport(new HttpClient(httpHandler));

                await WithConnectionAsync(
                    CreateConnection(httpHandler, transport: lp, transportType: HttpTransportType.LongPolling),
                    async (connection) =>
                    {
                        var ex = await Assert.ThrowsAsync<AggregateException>(async () => await connection.StartAsync(cts.Token).DefaultTimeout());
                    });
            }
        }

        private static async Task AssertDisposedAsync(HttpConnection connection)
        {
            var exception =
                await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.StartAsync());
            Assert.Equal(typeof(HttpConnection).FullName, exception.ObjectName);
        }
    }
}

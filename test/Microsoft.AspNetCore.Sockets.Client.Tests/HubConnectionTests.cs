// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionTests
    {
        [Fact]
        public void CannotCreateHubConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new HubConnection((Uri)null, Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public async Task CanDisposeNotStartedHubConnection()
        {
            await new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), new LoggerFactory())
                .DisposeAsync();
        }

        [Fact]
        public async Task CannotStartRunningHubConnection()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), Mock.Of<IInvocationAdapter>(), new LoggerFactory());

                try
                {
                    await hubConnection.StartAsync(longPollingTransport, httpClient);
                    var exception =
                        await Assert.ThrowsAsync<InvalidOperationException>(
                            async () => await hubConnection.StartAsync(longPollingTransport));
                    Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
                }
                finally
                {
                    await hubConnection.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task CannotStartStoppedHubConnection()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), Mock.Of<IInvocationAdapter>(), new LoggerFactory());

                await hubConnection.StartAsync(longPollingTransport, httpClient);
                await hubConnection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.StartAsync(longPollingTransport));

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact(Skip = "Not implemented")]
        public async Task InvokeThrowsIfHubConnectionNotStarted()
        {
            var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>());
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
            Assert.Equal("Cannot invoke methods on non-started connections.", exception.Message);
        }

        [Fact(Skip = "Not implemented")]
        public async Task InvokeThrowsIfHubConnectionDisposed()
        {
            var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>());
            await hubConnection.DisposeAsync();

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
                Assert.Equal("Cannot invoke methods on disposed connections.", exception.Message);
        }

        [Fact]
        public async Task HubConnectionConnectedEventRaisedWhenTheClientIsConnected()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), new LoggerFactory());
                try
                {
                    var connectedEventRaisedTcs = new TaskCompletionSource<object>();
                    hubConnection.Connected += () => connectedEventRaisedTcs.SetResult(null);

                    await hubConnection.StartAsync(longPollingTransport, httpClient);

                    await connectedEventRaisedTcs.Task.OrTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsStopped()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory());
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), new LoggerFactory());
                var closedEventTcs = new TaskCompletionSource<Exception>();
                hubConnection.Closed += e => closedEventTcs.SetResult(e);

                await hubConnection.StartAsync(longPollingTransport, httpClient);
                await hubConnection.DisposeAsync();

                Assert.Null(await closedEventTcs.Task.OrTimeout());
            }
        }

        [Fact]
        public async Task CannotCallInvokeOnClosedHubConnection()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .Setup(m => m.DisposeAsync())
                .Callback(() => mockConnection.Raise(c => c.Closed += null, (Exception)null))
                .Returns(Task.FromResult<object>(null));

            var hubConnection = new HubConnection(mockConnection.Object, Mock.Of<IInvocationAdapter>(), new LoggerFactory());

            await hubConnection.StartAsync(Mock.Of<ITransport>());
            await hubConnection.DisposeAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await hubConnection.Invoke("test", typeof(int)));

            Assert.Equal("Connection has been terminated.", exception.Message);
        }

        [Fact]
        public async Task PendingInvocationsAreCancelledWhenConnectionClosesCleanly()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .Setup(m => m.DisposeAsync())
                .Callback(() => mockConnection.Raise(c => c.Closed += null, (Exception)null))
                .Returns(Task.FromResult<object>(null));

            var hubConnection = new HubConnection(mockConnection.Object, Mock.Of<IInvocationAdapter>(), new LoggerFactory());

            await hubConnection.StartAsync(Mock.Of<ITransport>());
            var invokeTask = hubConnection.Invoke("testMethod", typeof(int));
            await hubConnection.DisposeAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }

        [Fact]
        public async Task PendingInvocationsAreTerminatedWithExceptionWhenConnectionClosesDueToError()
        {
            var exception = new InvalidOperationException();
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .Setup(m => m.DisposeAsync())
                .Callback(() => mockConnection.Raise(c => c.Closed += null, exception))
                .Returns(Task.FromResult<object>(null));

            var hubConnection = new HubConnection(mockConnection.Object, Mock.Of<IInvocationAdapter>(), new LoggerFactory());

            await hubConnection.StartAsync(Mock.Of<ITransport>());
            var invokeTask = hubConnection.Invoke("testMethod", typeof(int));
            await hubConnection.DisposeAsync();

            var thrown = await Assert.ThrowsAsync(exception.GetType(), async () => await invokeTask);
            Assert.Same(exception, thrown);
        }
    }
}

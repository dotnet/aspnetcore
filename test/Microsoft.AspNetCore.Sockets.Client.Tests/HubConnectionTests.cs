using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionTests
    {
        [Fact]
        public void CannotCreateHubConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new HubConnection(null, Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public void CanDisposeNotStartedHubConnection()
        {
            using (new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()))
            { }
        }

        [Fact]
        public async Task CanStopNotStartedHubConnection()
        {
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()))
            {
                await hubConnection.StopAsync();
            }
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), Mock.Of<IInvocationAdapter>(), new LoggerFactory()))
            {
                await hubConnection.StartAsync(longPollingTransport, httpClient);
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.StartAsync(longPollingTransport));
                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);

                await hubConnection.StopAsync();
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), Mock.Of<IInvocationAdapter>(), new LoggerFactory()))
            {
                await hubConnection.StartAsync(longPollingTransport, httpClient);
                await hubConnection.StopAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.StartAsync(longPollingTransport));

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task CannotStartDisposedHubConnection()
        {
            using (var httpClient = new HttpClient())
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            {
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), Mock.Of<IInvocationAdapter>(), new LoggerFactory());
                hubConnection.Dispose();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.StartAsync(longPollingTransport));

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact(Skip = "Not implemented")]
        public async Task InvokeThrowsIfHubConnectionNotStarted()
        {
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()))
            {
                var exception = 
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
                Assert.Equal("Cannot invoke methods on non-started connections.", exception.Message);
            }
        }

        [Fact(Skip = "Not implemented")]
        public async Task InvokeThrowsIfHubConnectionDisposed()
        {
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), Mock.Of<ILoggerFactory>()))
            {
                hubConnection.Dispose();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
                Assert.Equal("Cannot invoke methods on disposed connections.", exception.Message);
            }
        }

        // TODO: If HubConnection takes (I)Connection we could just tests if events are wired up 

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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), new LoggerFactory()))
            {
                var connectedEventRaised = false;
                hubConnection.Connected += () => connectedEventRaised = true;

                await hubConnection.StartAsync(longPollingTransport, httpClient);

                Assert.True(connectedEventRaised);
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
            using (var longPollingTransport = new LongPollingTransport(httpClient, new LoggerFactory()))
            using (var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), Mock.Of<IInvocationAdapter>(), new LoggerFactory()))
            {
                var closedEventTcs = new TaskCompletionSource<Exception>();
                hubConnection.Closed += e => closedEventTcs.SetResult(e);

                await hubConnection.StartAsync(longPollingTransport, httpClient);
                await hubConnection.StopAsync();


                Assert.Equal(closedEventTcs.Task, await Task.WhenAny(Task.Delay(1000), closedEventTcs.Task));
                // in case of clean disconnect error should be null
                Assert.Null(await closedEventTcs.Task);
            }
        }
    }
}

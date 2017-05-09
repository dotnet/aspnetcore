// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Tests;
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
                () => new HubConnection((Uri)null, Mock.Of<ILoggerFactory>()));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public async Task CanDisposeNotStartedHubConnection()
        {
            await new HubConnection(new Uri("http://fakeuri.org"), new LoggerFactory())
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
                    return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), new LoggerFactory());

                try
                {
                    await hubConnection.StartAsync(TransportType.LongPolling, httpClient);
                    var exception =
                        await Assert.ThrowsAsync<InvalidOperationException>(
                            async () => await hubConnection.StartAsync());
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
                    return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org/"), new LoggerFactory());

                await hubConnection.StartAsync(TransportType.LongPolling, httpClient);
                await hubConnection.DisposeAsync();
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.StartAsync());

                Assert.Equal("Cannot start a connection that is not in the Initial state.", exception.Message);
            }
        }

        [Fact]
        public async Task InvokeThrowsIfHubConnectionNotStarted()
        {
            var hubConnection = new HubConnection(new Uri("http://fakeuri.org"));
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task InvokeThrowsIfHubConnectionDisposed()
        {
            var hubConnection = new HubConnection(new Uri("http://fakeuri.org"));
            await hubConnection.DisposeAsync();

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
            Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
        }

        [Fact]
        public async Task InvokeThrowsIfSerializingMessageFails()
        {
            var mockConnection = new Mock<IConnection>();

            var exception = new InvalidOperationException();
            var mockProtocol = MockHubProtocol.Throw(exception);
            var hubConnection = new HubConnection(mockConnection.Object, mockProtocol, null);
            await hubConnection.StartAsync(TransportType.All, httpClient: null);

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
            Assert.Same(exception, actualException);
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
                    return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), new LoggerFactory());
                try
                {
                    var connectedEventRaisedTcs = new TaskCompletionSource<object>();
                    hubConnection.Connected += () => connectedEventRaisedTcs.SetResult(null);

                    await hubConnection.StartAsync(TransportType.LongPolling, httpClient);

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
                    return ResponseUtils.CreateResponse(System.Net.HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var hubConnection = new HubConnection(new Uri("http://fakeuri.org"), new LoggerFactory());
                var closedEventTcs = new TaskCompletionSource<Exception>();
                hubConnection.Closed += e => closedEventTcs.SetResult(e);

                await hubConnection.StartAsync(TransportType.LongPolling, httpClient);
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

            var hubConnection = new HubConnection(mockConnection.Object, new LoggerFactory());

            await hubConnection.StartAsync(new TestTransportFactory(Mock.Of<ITransport>()), httpClient: null);
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

            var hubConnection = new HubConnection(mockConnection.Object, new LoggerFactory());

            await hubConnection.StartAsync(new TestTransportFactory(Mock.Of<ITransport>()), httpClient: null);
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

            var hubConnection = new HubConnection(mockConnection.Object, new LoggerFactory());

            await hubConnection.StartAsync(new TestTransportFactory(Mock.Of<ITransport>()), httpClient: null);
            var invokeTask = hubConnection.Invoke("testMethod", typeof(int));
            await hubConnection.DisposeAsync();

            var thrown = await Assert.ThrowsAsync(exception.GetType(), async () => await invokeTask);
            Assert.Same(exception, thrown);
        }

        [Fact]
        public async Task DoesNotThrowWhenClientMethodCalledButNoInvocationHandlerHasBeenSetUp()
        {
            var mockConnection = new Mock<IConnection>();

            var invocation = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: "NonExistingMethod123", arguments: new object[] { true, "arg2", 123 });

            var mockProtocol = MockHubProtocol.ReturnOnParse(invocation);

            var hubConnection = new HubConnection(mockConnection.Object, mockProtocol, null);
            await hubConnection.StartAsync(new TestTransportFactory(Mock.Of<ITransport>()), httpClient: null);

            mockConnection.Raise(c => c.Received += null, new object[] { new byte[] { }, MessageType.Text });
            Assert.Equal(1, mockProtocol.ParseCalls);
        }

        // Moq really doesn't handle out parameters well, so to make these tests work I added a manual mock -anurse
        private class MockHubProtocol : IHubProtocol
        {
            private HubMessage _parsed;
            private Exception _error;

            public int ParseCalls { get; private set; } = 0;
            public int WriteCalls { get; private set; } = 0;

            public MessageType MessageType => MessageType.Text;

            public static MockHubProtocol ReturnOnParse(HubMessage parsed)
            {
                return new MockHubProtocol
                {
                    _parsed = parsed
                };
            }

            public static MockHubProtocol Throw(Exception error)
            {
                return new MockHubProtocol
                {
                    _error = error
                };
            }

            public HubMessage ParseMessage(ReadOnlySpan<byte> input, IInvocationBinder binder)
            {
                ParseCalls += 1;
                if (_error != null)
                {
                    throw _error;
                }
                if (_parsed != null)
                {
                    return _parsed;
                }

                throw new InvalidOperationException("No Parsed Message provided");
            }

            public bool TryWriteMessage(HubMessage message, IOutput output)
            {
                WriteCalls += 1;

                if (_error != null)
                {
                    throw _error;
                }
                return true;
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubEndpointTests
    {
        [Fact]
        public async Task HubsAreDisposed()
        {
            var trackDispose = new TrackDispose();
            var serviceProvider = CreateServiceProvider(s => s.AddSingleton(trackDispose));
            var endPoint = serviceProvider.GetService<HubEndPoint<TestHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.HttpConnection.Input.ReadingStarted;

                // kill the connection
                connectionWrapper.Connection.Channel.Dispose();

                await endPointTask;

                Assert.Equal(2, trackDispose.DisposeCount);
            }
        }

        [Fact]
        public async Task OnDisconnectedCalledWithExceptionIfHubMethodNotFound()
        {
            var hub = Mock.Of<Hub>();

            var endPointType = GetEndPointType(hub.GetType());
            var serviceProvider = CreateServiceProvider(s =>
            {
                s.AddSingleton(endPointType);
                s.AddTransient(hub.GetType(), sp => hub);
            });

            dynamic endPoint = serviceProvider.GetService(endPointType);

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.HttpConnection.Input.ReadingStarted;

                var buffer = connectionWrapper.HttpConnection.Input.Alloc();
                buffer.Write(Encoding.UTF8.GetBytes("0xdeadbeef"));
                await buffer.FlushAsync();

                connectionWrapper.Connection.Channel.Dispose();

                // InvalidCastException because the payload is not a JObject
                // which is expected by the formatter
                await Assert.ThrowsAsync<InvalidCastException>(async () => await endPointTask);

                Mock.Get(hub).Verify(h => h.OnDisconnectedAsync(It.IsNotNull<Exception>()), Times.Once());
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfLifetimeManagerOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<Hub>>();
            mockLifetimeManager
                .Setup(m => m.OnConnectedAsync(It.IsAny<Connection>()))
                .Throws(new InvalidOperationException("Lifetime manager OnConnectedAsync failed."));
            var mockHubActivator = new Mock<IHubActivator<Hub, IClientProxy>>();

            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
                services.AddSingleton(mockHubActivator.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<Hub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await endPoint.OnConnectedAsync(connectionWrapper.Connection));
                Assert.Equal("Lifetime manager OnConnectedAsync failed.", exception.Message);

                connectionWrapper.Connection.Channel.Dispose();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<Connection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<Connection>()), Times.Once);
                // No hubs should be created since the connection is terminated
                mockHubActivator.Verify(m => m.Create(), Times.Never);
                mockHubActivator.Verify(m => m.Release(It.IsAny<Hub>()), Times.Never);
            }
        }

        [Fact]
        public async Task HubOnDisconnectedAsyncCalledIfHubOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<OnConnectedThrowsHub>>();
            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<OnConnectedThrowsHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);
                connectionWrapper.Connection.Channel.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnConnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<Connection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<Connection>()), Times.Once);
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfHubOnDisconnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<OnDisconnectedThrowsHub>>();
            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<OnDisconnectedThrowsHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);
                connectionWrapper.Connection.Channel.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<Connection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<Connection>()), Times.Once);
            }
        }

        private static Type GetEndPointType(Type hubType)
        {
            var endPointType = typeof(HubEndPoint<>);
            return endPointType.MakeGenericType(hubType);
        }

        private static Type GetGenericType(Type genericType, Type hubType)
        {
            return genericType.MakeGenericType(hubType);
        }

        public class OnConnectedThrowsHub : Hub
        {
            public override Task OnConnectedAsync()
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new InvalidOperationException("Hub OnConnected failed."));
                return tcs.Task;
            }
        }

        public class OnDisconnectedThrowsHub : Hub
        {
            public override Task OnDisconnectedAsync(Exception exception)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new InvalidOperationException("Hub OnDisconnected failed."));
                return tcs.Task;
            }
        }

        private class TestHub : Hub
        {
            private TrackDispose _trackDispose;

            public TestHub(TrackDispose trackDispose)
            {
                _trackDispose = trackDispose;
            }

            protected override void Dispose(bool dispose)
            {
                if (dispose)
                {
                    _trackDispose.DisposeCount++;
                }
            }
        }

        private class TrackDispose
        {
            public int DisposeCount = 0;
        }

        private IServiceProvider CreateServiceProvider(Action<ServiceCollection> addServices = null)
        {
            var services = new ServiceCollection();
            services.AddOptions()
                .AddLogging()
                .AddSignalR();

            addServices?.Invoke(services);

            return services.BuildServiceProvider();
        }

        private class ConnectionWrapper : IDisposable
        {
            private PipelineFactory _factory;
            private HttpConnection _httpConnection;

            public Connection Connection;
            public HttpConnection HttpConnection => (HttpConnection)Connection.Channel;

            public ConnectionWrapper(string format = "json")
            {
                _factory = new PipelineFactory();
                _httpConnection = new HttpConnection(_factory);

                var connectionManager = new ConnectionManager();

                Connection = connectionManager.AddNewConnection(_httpConnection).Connection;
                Connection.Metadata["formatType"] = format;
                Connection.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            public void Dispose()
            {
                Connection.Channel.Dispose();
                _httpConnection.Dispose();
                _factory.Dispose();
            }
        }
    }
}

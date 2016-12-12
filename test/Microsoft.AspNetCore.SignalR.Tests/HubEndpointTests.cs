// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Moq;
using Newtonsoft.Json;
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

                await connectionWrapper.ApplicationStartedReading;

                // kill the connection
                connectionWrapper.ConnectionState.Dispose();

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

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");
                await SendRequest(connectionWrapper.Connection.Transport, adapter, "0xdeadbeef");

                connectionWrapper.Dispose();

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);

                Mock.Get(hub).Verify(h => h.OnDisconnectedAsync(It.IsNotNull<InvalidOperationException>()), Times.Once());
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfLifetimeManagerOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<Hub>>();
            mockLifetimeManager
                .Setup(m => m.OnConnectedAsync(It.IsAny<StreamingConnection>()))
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

                connectionWrapper.ConnectionState.Dispose();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
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
                connectionWrapper.ConnectionState.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnConnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
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
                connectionWrapper.ConnectionState.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<StreamingConnection>()), Times.Once);
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

        [Fact]
        public async Task HubMethodCanReturnValueFromTask()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "TaskValueMethod");
                var res = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper.Connection.Transport);
                // json serializer makes this a long
                Assert.Equal(42L, res.Result);

                // kill the connection
                connectionWrapper.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValue()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "ValueMethod");
                var res = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper.Connection.Transport);
                // json serializer makes this a long
                Assert.Equal(43L, res.Result);

                // kill the connection
                connectionWrapper.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task HubMethodCanBeStatic()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "StaticMethod");
                var res = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper.Connection.Transport);
                Assert.Equal("fromStatic", res.Result);

                // kill the connection
                connectionWrapper.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task HubMethodCanBeVoid()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "VoidMethod");
                var res = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper.Connection.Transport);
                Assert.Equal(null, res.Result);

                // kill the connection
                connectionWrapper.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task HubMethodWithMultiParam()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "ConcatString", (byte)32, 42, 'm', "string");
                var res = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper.Connection.Transport);
                Assert.Equal("32, 42, m, string", res.Result);

                // kill the connection
                connectionWrapper.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task CannotCallOverriddenBaseHubMethod()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connectionWrapper = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connectionWrapper.Connection);

                await connectionWrapper.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(connectionWrapper.Connection.Transport, adapter, "OnDisconnectedAsync");

                try
                {
                    await endPointTask;
                    Assert.True(false);
                }
                catch (InvalidOperationException ex)
                {
                    Assert.Equal("The hub method 'OnDisconnectedAsync' could not be resolved.", ex.Message);
                }
            }
        }

        [Fact]
        public async Task BroadcastHubMethod_SendsToAllClients()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstConnection = new ConnectionWrapper())
            using (var secondConnection = new ConnectionWrapper())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstConnection.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondConnection.Connection);

                await Task.WhenAll(firstConnection.ApplicationStartedReading, secondConnection.ApplicationStartedReading);

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest(firstConnection.Connection.Transport, adapter, "BroadcastMethod", "test");

                foreach (var res in await Task.WhenAll(
                    ReadConnectionOutputAsync<InvocationDescriptor>(firstConnection.Connection.Transport),
                    ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection.Connection.Transport)))
                {
                    Assert.Equal("Broadcast", res.Method);
                    Assert.Equal(1, res.Arguments.Length);
                    Assert.Equal("test", res.Arguments[0]);
                }

                // kill the connections
                firstConnection.Connection.Dispose();
                secondConnection.Connection.Dispose();
                
                await Task.WhenAll(firstEndPointTask, secondEndPointTask);
            }
        }

        [Fact]
        public async Task HubsCanAddAndSendToGroup()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstConnection = new ConnectionWrapper())
            using (var secondConnection = new ConnectionWrapper())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstConnection.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondConnection.Connection);

                await Task.WhenAll(firstConnection.ApplicationStartedReading, secondConnection.ApplicationStartedReading);

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest_IgnoreReceive(firstConnection.Connection.Transport, adapter, "GroupSendMethod", "testGroup", "test");
                // check that 'secondConnection' hasn't received the group send
                Assert.False(((PipelineReaderWriter)secondConnection.Connection.Transport.Output).ReadAsync().IsCompleted);

                await SendRequest_IgnoreReceive(secondConnection.Connection.Transport, adapter, "GroupAddMethod", "testGroup");

                await SendRequest(firstConnection.Connection.Transport, adapter, "GroupSendMethod", "testGroup", "test");
                // check that 'firstConnection' hasn't received the group send
                Assert.False(((PipelineReaderWriter)firstConnection.Connection.Transport.Output).ReadAsync().IsCompleted);

                // check that 'secondConnection' has received the group send
                var res = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection.Connection.Transport);
                Assert.Equal("Send", res.Method);
                Assert.Equal(1, res.Arguments.Length);
                Assert.Equal("test", res.Arguments[0]);

                // kill the connections
                firstConnection.Connection.Dispose();
                secondConnection.Connection.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask);
            }
        }

        [Fact]
        public async Task RemoveFromGroupWhenNotInGroupDoesNotFail()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var connection = new ConnectionWrapper())
            {
                var endPointTask = endPoint.OnConnectedAsync(connection.Connection);

                await connection.ApplicationStartedReading;

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var writer = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest_IgnoreReceive(connection.Connection.Transport, writer, "GroupRemoveMethod", "testGroup");

                // kill the connection
                connection.Connection.Dispose();

                await endPointTask;
            }
        }

        [Fact]
        public async Task HubsCanSendToUser()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstConnection = new ConnectionWrapper())
            using (var secondConnection = new ConnectionWrapper())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstConnection.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondConnection.Connection);

                await Task.WhenAll(firstConnection.ApplicationStartedReading, secondConnection.ApplicationStartedReading);

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest_IgnoreReceive(firstConnection.Connection.Transport, adapter, "ClientSendMethod", secondConnection.Connection.User.Identity.Name, "test");

                // check that 'secondConnection' has received the group send
                var res = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection.Connection.Transport);
                Assert.Equal("Send", res.Method);
                Assert.Equal(1, res.Arguments.Length);
                Assert.Equal("test", res.Arguments[0]);

                // kill the connections
                firstConnection.Connection.Dispose();
                secondConnection.Connection.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask);
            }
        }

        [Fact]
        public async Task HubsCanSendToConnection()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstConnection = new ConnectionWrapper())
            using (var secondConnection = new ConnectionWrapper())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstConnection.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondConnection.Connection);

                await Task.WhenAll(firstConnection.ApplicationStartedReading, secondConnection.ApplicationStartedReading);

                var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
                var adapter = invocationAdapter.GetInvocationAdapter("json");

                await SendRequest_IgnoreReceive(firstConnection.Connection.Transport, adapter, "ConnectionSendMethod", secondConnection.Connection.ConnectionId, "test");

                // check that 'secondConnection' has received the group send
                var res = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection.Connection.Transport);
                Assert.Equal("Send", res.Method);
                Assert.Equal(1, res.Arguments.Length);
                Assert.Equal("test", res.Arguments[0]);

                // kill the connections
                firstConnection.Connection.Dispose();
                secondConnection.Connection.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask);
            }
        }

        private class MethodHub : Hub
        {
            public Task GroupRemoveMethod(string groupName)
            {
                return Groups.RemoveAsync(groupName);
            }

            public Task ClientSendMethod(string userId, string message)
            {
                return Clients.User(userId).InvokeAsync("Send", message);
            }

            public Task ConnectionSendMethod(string connectionId, string message)
            {
                return Clients.Client(connectionId).InvokeAsync("Send", message);
            }

            public Task GroupAddMethod(string groupName)
            {
                return Groups.AddAsync(groupName);
            }

            public Task GroupSendMethod(string groupName, string message)
            {
                return Clients.Group(groupName).InvokeAsync("Send", message);
            }

            public Task BroadcastMethod(string message)
            {
                return Clients.All.InvokeAsync("Broadcast", message);
            }

            public Task<int> TaskValueMethod()
            {
                return Task.FromResult(42);
            }

            public int ValueMethod()
            {
                return 43;
            }

            static public string StaticMethod()
            {
                return "fromStatic";
            }

            public void VoidMethod()
            {
            }

            public string ConcatString(byte b, int i, char c, string s)
            {
                return $"{b}, {i}, {c}, {s}";
            }

            public override Task OnDisconnectedAsync(Exception e)
            {
                return TaskCache.CompletedTask;
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

        public async Task SendRequest(IPipelineConnection connection, IInvocationAdapter writer, string method, params object[] args)
        {
            if (connection == null)
            {
                throw new ArgumentNullException();
            }

            var stream = new MemoryStream();
            await writer.WriteMessageAsync(new InvocationDescriptor
            {
                Arguments = args,
                Method = method
            }, stream);

            var buffer = ((PipelineReaderWriter)connection.Input).Alloc();
            buffer.Write(stream.ToArray());
            await buffer.FlushAsync();
        }

        public async Task SendRequest_IgnoreReceive(IPipelineConnection connection, IInvocationAdapter writer, string method, params object[] args)
        {
            await SendRequest(connection, writer, method, args);

            var methodResult = await ((PipelineReaderWriter)connection.Output).ReadAsync();
            ((PipelineReaderWriter)connection.Output).AdvanceReader(methodResult.Buffer.End, methodResult.Buffer.End);
        }

        private async Task<T> ReadConnectionOutputAsync<T>(IPipelineConnection connection)
        {
            // TODO: other formats?
            var methodResult = await ((PipelineReaderWriter)connection.Output).ReadAsync();
            var serializer = new JsonSerializer();
            var res = serializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(methodResult.Buffer.ToArray()))));
            ((PipelineReaderWriter)connection.Output).AdvanceReader(methodResult.Buffer.End, methodResult.Buffer.End);

            return res;
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
            private static int ID;
            private PipelineFactory _factory;

            public StreamingConnectionState ConnectionState;

            public StreamingConnection Connection => ConnectionState.Connection;

            // Still kinda gross...
            public Task ApplicationStartedReading => ((PipelineReaderWriter)Connection.Transport.Input).ReadingStarted;

            public ConnectionWrapper(string format = "json")
            {
                _factory = new PipelineFactory();

                var connectionManager = new ConnectionManager(_factory);

                ConnectionState = (StreamingConnectionState)connectionManager.CreateConnection(ConnectionMode.Streaming);
                ConnectionState.Connection.Metadata["formatType"] = format;
                ConnectionState.Connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Interlocked.Increment(ref ID).ToString()) }));
            }

            public void Dispose()
            {
                ConnectionState.Dispose();
                _factory.Dispose();
            }
        }
    }
}

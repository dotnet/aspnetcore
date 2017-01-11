// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
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
                connectionWrapper.Dispose();

                await endPointTask;

                Assert.Equal(2, trackDispose.DisposeCount);
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

                connectionWrapper.Dispose();

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
                connectionWrapper.Dispose();

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
                connectionWrapper.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<Connection>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<Connection>()), Times.Once);
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

                await SendRequest(connectionWrapper, adapter, nameof(MethodHub.TaskValueMethod));
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);

                // json serializer makes this a long
                Assert.Equal(42L, result.Result);

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

                await SendRequest(connectionWrapper, adapter, "ValueMethod");
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);

                // json serializer makes this a long
                Assert.Equal(43L, result.Result);

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

                await SendRequest(connectionWrapper, adapter, "StaticMethod");
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);

                Assert.Equal("fromStatic", result.Result);

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

                await SendRequest(connectionWrapper, adapter, "VoidMethod");
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);

                Assert.Null(result.Result);

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

                await SendRequest(connectionWrapper, adapter, "ConcatString", (byte)32, 42, 'm', "string");
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);
                Assert.Equal("32, 42, m, string", result.Result);

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

                await SendRequest(connectionWrapper, adapter, "OnDisconnectedAsync");
                var result = await ReadConnectionOutputAsync<InvocationResultDescriptor>(connectionWrapper);

                Assert.Equal("Unknown hub method 'OnDisconnectedAsync'", result.Error);
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

                await SendRequest(firstConnection, adapter, "BroadcastMethod", "test");

                foreach (var result in await Task.WhenAll(
                    ReadConnectionOutputAsync<InvocationDescriptor>(firstConnection),
                    ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection)))
                {
                    Assert.Equal("Broadcast", result.Method);
                    Assert.Equal(1, result.Arguments.Length);
                    Assert.Equal("test", result.Arguments[0]);
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

                await SendRequest_IgnoreReceive(firstConnection, adapter, "GroupSendMethod", "testGroup", "test");
                // check that 'secondConnection' hasn't received the group send
                Message message;
                Assert.False(secondConnection.Transport.Output.TryRead(out message));

                await SendRequest_IgnoreReceive(secondConnection, adapter, "GroupAddMethod", "testGroup");

                await SendRequest(firstConnection, adapter, "GroupSendMethod", "testGroup", "test");

                // check that 'firstConnection' hasn't received the group send
                Assert.False(firstConnection.Transport.Output.TryRead(out message));

                // check that 'secondConnection' has received the group send
                var res = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection);
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

                await SendRequest_IgnoreReceive(connection, writer, "GroupRemoveMethod", "testGroup");

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

                await SendRequest_IgnoreReceive(firstConnection, adapter, "ClientSendMethod", secondConnection.Connection.User.Identity.Name, "test");

                // check that 'secondConnection' has received the group send
                var res = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection);
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

                await SendRequest_IgnoreReceive(firstConnection, adapter, "ConnectionSendMethod", secondConnection.Connection.ConnectionId, "test");

                // check that 'secondConnection' has received the group send
                var result = await ReadConnectionOutputAsync<InvocationDescriptor>(secondConnection);
                Assert.Equal("Send", result.Method);
                Assert.Equal(1, result.Arguments.Length);
                Assert.Equal("test", result.Arguments[0]);

                // kill the connections
                firstConnection.Connection.Dispose();
                secondConnection.Connection.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask);
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

        public async Task SendRequest(ConnectionWrapper connection, IInvocationAdapter writer, string method, params object[] args)
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
            },
            stream);

            var buffer = ReadableBuffer.Create(stream.ToArray()).Preserve();
            await connection.Transport.Input.WriteAsync(new Message(buffer, Format.Binary, endOfMessage: true));
        }

        public async Task SendRequest_IgnoreReceive(ConnectionWrapper connection, IInvocationAdapter writer, string method, params object[] args)
        {
            await SendRequest(connection, writer, method, args);

            // Consume the result
            await connection.Transport.Output.ReadAsync();
        }

        private async Task<T> ReadConnectionOutputAsync<T>(ConnectionWrapper connection)
        {
            // TODO: other formats?
            var message = await connection.Transport.Output.ReadAsync();
            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(message.Payload.Buffer.ToArray()))));
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

        public class ConnectionWrapper : IDisposable
        {
            private static int _id;
            private readonly TestChannel<Message> _input;

            public Connection Connection { get; }

            public ChannelConnection<Message> Transport { get; }

            public Task ApplicationStartedReading => _input.ReadingStarted;

            public ConnectionWrapper(string format = "json")
            {
                var transportToApplication = Channel.CreateUnbounded<Message>();
                var applicationToTransport = Channel.CreateUnbounded<Message>();

                _input = new TestChannel<Message>(transportToApplication);

                Transport = new ChannelConnection<Message>(_input, applicationToTransport);

                Connection = new Connection(Guid.NewGuid().ToString(), Transport);
                Connection.Metadata["formatType"] = format;
                Connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Interlocked.Increment(ref _id).ToString()) }));
            }

            public void Dispose()
            {
                Connection.Dispose();
            }

            private class TestChannel<T> : IChannel<T>
            {
                private IChannel<T> _channel;
                private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

                public TestChannel(IChannel<T> channel)
                {
                    _channel = channel;
                }

                public Task Completion => _channel.Completion;

                public Task ReadingStarted => _tcs.Task;

                public ValueAwaiter<T> GetAwaiter()
                {
                    return _channel.GetAwaiter();
                }

                public ValueTask<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    _tcs.TrySetResult(null);
                    return _channel.ReadAsync(cancellationToken);
                }

                public bool TryComplete(Exception error = null)
                {
                    return _channel.TryComplete(error);
                }

                public bool TryRead(out T item)
                {
                    return _channel.TryRead(out item);
                }

                public bool TryWrite(T item)
                {
                    return _channel.TryWrite(item);
                }

                public Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    _tcs.TrySetResult(null);
                    return _channel.WaitToReadAsync(cancellationToken);
                }

                public Task<bool> WaitToWriteAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    return _channel.WaitToWriteAsync(cancellationToken);
                }

                public Task WriteAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
                {
                    return _channel.WriteAsync(item, cancellationToken);
                }
            }
        }
    }
}

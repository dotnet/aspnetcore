// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.Common;
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
            var endPoint = serviceProvider.GetService<HubEndPoint<DisposeTrackingHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                // kill the connection
                client.Dispose();

                await endPointTask;

                Assert.Equal(2, trackDispose.DisposeCount);
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfLifetimeManagerOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<Hub>>();
            mockLifetimeManager
                .Setup(m => m.OnConnectedAsync(It.IsAny<ConnectionContext>()))
                .Throws(new InvalidOperationException("Lifetime manager OnConnectedAsync failed."));
            var mockHubActivator = new Mock<IHubActivator<Hub, IClientProxy>>();

            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
                services.AddSingleton(mockHubActivator.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<Hub>>();

            using (var client = new TestClient())
            {
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await endPoint.OnConnectedAsync(client.Connection));
                Assert.Equal("Lifetime manager OnConnectedAsync failed.", exception.Message);

                client.Dispose();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
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

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);
                client.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnConnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
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

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);
                client.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<ConnectionContext>()), Times.Once);
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValueFromTask()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(MethodHub.TaskValueMethod)).OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(42L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodsAreCaseInsensitive()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync("echo", "hello").OrTimeout()).Result;

                Assert.Equal("hello", result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Theory]
        [InlineData(nameof(MethodHub.MethodThatThrows))]
        [InlineData(nameof(MethodHub.MethodThatYieldsFailedTask))]
        public async Task HubMethodCanThrowOrYieldFailedTask(string methodName)
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(methodName).OrTimeout());

                Assert.Equal("BOOM!", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValue()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(MethodHub.ValueMethod)).OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(43L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanBeVoid()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(MethodHub.VoidMethod)).OrTimeout()).Result;

                Assert.Null(result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodWithMultiParam()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(MethodHub.ConcatString), (byte)32, 42, 'm', "string").OrTimeout()).Result;

                Assert.Equal("32, 42, m, string", result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanCallInheritedHubMethodFromInheritingHub()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<InheritedHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(InheritedHub.BaseMethod), "string").OrTimeout()).Result;

                Assert.Equal("string", result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanCallOverridenVirtualHubMethod()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<InheritedHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = (await client.InvokeAsync(nameof(InheritedHub.VirtualMethod), 10).OrTimeout()).Result;

                Assert.Equal(0L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallOverriddenBaseHubMethod()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = await client.InvokeAsync(nameof(MethodHub.OnDisconnectedAsync)).OrTimeout();

                Assert.Equal("Unknown hub method 'OnDisconnectedAsync'", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public void HubsCannotHaveOverloadedMethods()
        {
            var serviceProvider = CreateServiceProvider();

            try
            {
                var endPoint = serviceProvider.GetService<HubEndPoint<InvalidHub>>();
                Assert.True(false);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal("Duplicate definitions of 'OverloadedMethod'. Overloading is not supported.", ex.Message);
            }
        }

        [Fact]
        public async Task CannotCallStaticHubMethods()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = await client.InvokeAsync(nameof(MethodHub.StaticMethod)).OrTimeout();

                Assert.Equal("Unknown hub method 'StaticMethod'", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallObjectMethodsOnHub()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = await client.InvokeAsync(nameof(MethodHub.ToString)).OrTimeout();
                Assert.Equal("Unknown hub method 'ToString'", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.GetHashCode)).OrTimeout();
                Assert.Equal("Unknown hub method 'GetHashCode'", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.Equals)).OrTimeout();
                Assert.Equal("Unknown hub method 'Equals'", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.ReferenceEquals)).OrTimeout();
                Assert.Equal("Unknown hub method 'ReferenceEquals'", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallDisposeMethodOnHub()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                var result = await client.InvokeAsync(nameof(MethodHub.Dispose)).OrTimeout();

                Assert.Equal("Unknown hub method 'Dispose'", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task BroadcastHubMethod_SendsToAllClients()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstClient.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondClient.Connection);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.BroadcastMethod), "test").OrTimeout();

                foreach (var result in await Task.WhenAll(
                    firstClient.Read(),
                    secondClient.Read()).OrTimeout())
                {
                    var invocation = Assert.IsType<InvocationMessage>(result);
                    Assert.Equal("Broadcast", invocation.Target);
                    Assert.Equal(1, invocation.Arguments.Length);
                    Assert.Equal("test", invocation.Arguments[0]);
                }

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Fact]
        public async Task HubsCanAddAndSendToGroup()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstClient.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondClient.Connection);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                var result = (await firstClient.InvokeAsync(nameof(MethodHub.GroupSendMethod), "testGroup", "test").OrTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                result = (await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout()).Result;

                await firstClient.SendInvocationAsync(nameof(MethodHub.GroupSendMethod), "testGroup", "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.Read().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal(1, invocation.Arguments.Length);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Fact]
        public async Task RemoveFromGroupWhenNotInGroupDoesNotFail()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = endPoint.OnConnectedAsync(client.Connection);

                await client.SendInvocationAsync(nameof(MethodHub.GroupRemoveMethod), "testGroup").OrTimeout();

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubsCanSendToUser()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstClient.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondClient.Connection);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.ClientSendMethod), secondClient.Connection.User.Identity.Name, "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.Read().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal(1, invocation.Arguments.Length);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Fact]
        public async Task HubsCanSendToConnection()
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstEndPointTask = endPoint.OnConnectedAsync(firstClient.Connection);
                var secondEndPointTask = endPoint.OnConnectedAsync(secondClient.Connection);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.ConnectionSendMethod), secondClient.Connection.ConnectionId, "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.Read().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal(1, invocation.Arguments.Length);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [InlineData(nameof(StreamingHub.CounterChannel))]
        [InlineData(nameof(StreamingHub.CounterObservable))]
        public async Task HubsCanStreamResponses(string method)
        {
            var serviceProvider = CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<StreamingHub>>();

            using (var client = new TestClient())
            {
                var endPointLifetime = endPoint.OnConnectedAsync(client.Connection);

                await client.Connected.OrTimeout();

                var messages = await client.StreamAsync(method, 4).OrTimeout();

                Assert.Equal(5, messages.Count);
                AssertHubMessage(new StreamItemMessage(string.Empty, "0"), messages[0]);
                AssertHubMessage(new StreamItemMessage(string.Empty, "1"), messages[1]);
                AssertHubMessage(new StreamItemMessage(string.Empty, "2"), messages[2]);
                AssertHubMessage(new StreamItemMessage(string.Empty, "3"), messages[3]);
                AssertHubMessage(new CompletionMessage(string.Empty, error: null, result: null, hasResult: false), messages[4]);

                client.Dispose();

                await endPointLifetime.OrTimeout();
            }
        }

        [Fact]
        public async Task UnauthorizedConnectionCannotInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("test", policy =>
                    {
                        policy.RequireClaim(ClaimTypes.NameIdentifier);
                        policy.AddAuthenticationSchemes("Default");
                    });
                });
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointLifetime = endPoint.OnConnectedAsync(client.Connection);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.NotNull(message.Error);

                client.Dispose();

                await endPointLifetime.OrTimeout();
            }
        }

        [Fact]
        public async Task AuthorizedConnectionCanInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = CreateServiceProvider(services =>
            {
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("test", policy =>
                    {
                        policy.RequireClaim(ClaimTypes.NameIdentifier);
                        policy.AddAuthenticationSchemes("Default");
                    });
                });
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                client.Connection.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));
                var endPointLifetime = endPoint.OnConnectedAsync(client.Connection);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await endPointLifetime.OrTimeout();
            }
        }

        private static void AssertHubMessage(HubMessage expected, HubMessage actual)
        {
            // We aren't testing InvocationIds here
            switch (expected)
            {
                case CompletionMessage expectedCompletion:
                    var actualCompletion = Assert.IsType<CompletionMessage>(actual);
                    Assert.Equal(expectedCompletion.Error, actualCompletion.Error);
                    Assert.Equal(expectedCompletion.HasResult, actualCompletion.HasResult);
                    Assert.Equal(expectedCompletion.Result, actualCompletion.Result);
                    break;
                case StreamItemMessage expectedStreamItem:
                    var actualStreamItem = Assert.IsType<StreamItemMessage>(actual);
                    Assert.Equal(expectedStreamItem.Item, actualStreamItem.Item);
                    break;
                case InvocationMessage expectedInvocation:
                    var actualInvocation = Assert.IsType<InvocationMessage>(actual);
                    Assert.Equal(expectedInvocation.NonBlocking, actualInvocation.NonBlocking);
                    Assert.Equal(expectedInvocation.Target, actualInvocation.Target);
                    Assert.Equal(expectedInvocation.Arguments, actualInvocation.Arguments);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Hub Message type {expected.GetType()}");
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

        private IServiceProvider CreateServiceProvider(Action<ServiceCollection> addServices = null)
        {
            var services = new ServiceCollection();
            services.AddOptions()
                .AddLogging()
                .AddSignalR();

            addServices?.Invoke(services);

            return services.BuildServiceProvider();
        }

        public class StreamingHub : TestHub
        {
            public IObservable<string> CounterObservable(int count)
            {
                return new CountingObservable(count);
            }

            public ReadableChannel<string> CounterChannel(int count)
            {
                var channel = Channel.CreateUnbounded<string>();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        await channel.Out.WriteAsync(i.ToString());
                    }
                    channel.Out.Complete();
                });

                return channel.In;
            }

            private class CountingObservable : IObservable<string>
            {
                private int _count;

                public CountingObservable(int count)
                {
                    _count = count;
                }

                public IDisposable Subscribe(IObserver<string> observer)
                {
                    var cts = new CancellationTokenSource();
                    Task.Run(() =>
                    {
                        for (int i = 0; !cts.Token.IsCancellationRequested && i < _count; i++)
                        {
                            observer.OnNext(i.ToString());
                        }
                        observer.OnCompleted();
                    });

                    return new CancellationDisposable(cts);
                }
            }
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

        public class OnDisconnectedThrowsHub : TestHub
        {
            public override Task OnDisconnectedAsync(Exception exception)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new InvalidOperationException("Hub OnDisconnected failed."));
                return tcs.Task;
            }
        }

        private class MethodHub : TestHub
        {
            public Task GroupRemoveMethod(string groupName)
            {
                return Groups.RemoveAsync(Context.ConnectionId, groupName);
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
                return Groups.AddAsync(Context.ConnectionId, groupName);
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

            public string Echo(string data)
            {
                return data;
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
                return Task.CompletedTask;
            }

            public void MethodThatThrows()
            {
                throw new InvalidOperationException("BOOM!");
            }

            public Task MethodThatYieldsFailedTask()
            {
                return Task.FromException(new InvalidOperationException("BOOM!"));
            }

            public static void StaticMethod()
            {
            }

            [Authorize("test")]
            public void AuthMethod()
            {
            }
        }

        private class InheritedHub : BaseHub
        {
            public override int VirtualMethod(int num)
            {
                return num - 10;
            }
        }

        private class BaseHub : TestHub
        {
            public string BaseMethod(string message)
            {
                return message;
            }

            public virtual int VirtualMethod(int num)
            {
                return num;
            }
        }

        private class InvalidHub : TestHub
        {
            public void OverloadedMethod(int num)
            {
            }

            public void OverloadedMethod(string message)
            {
            }
        }

        private class DisposeTrackingHub : TestHub
        {
            private TrackDispose _trackDispose;

            public DisposeTrackingHub(TrackDispose trackDispose)
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

        public abstract class TestHub : Hub
        {
            public override Task OnConnectedAsync()
            {
                Context.Connection.Metadata.Get<TaskCompletionSource<bool>>("ConnectedTask")?.TrySetResult(true);
                return base.OnConnectedAsync();
            }
        }
    }
}

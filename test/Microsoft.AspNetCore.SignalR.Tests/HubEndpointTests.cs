// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.HubEndpointTestUtils;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubEndpointTests
    {
        [Fact]
        public async Task HubsAreDisposed()
        {
            var trackDispose = new TrackDispose();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(s => s.AddSingleton(trackDispose));
            var endPoint = serviceProvider.GetService<HubEndPoint<DisposeTrackingHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                // kill the connection
                client.Dispose();

                await endPointTask;

                Assert.Equal(2, trackDispose.DisposeCount);
            }
        }

        [Fact]
        public async Task ConnectionAbortedTokenTriggers()
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(s => s.AddSingleton(state));
            var endPoint = serviceProvider.GetService<HubEndPoint<ConnectionLifetimeHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();

                Assert.True(state.TokenCallbackTriggered);
                Assert.False(state.TokenStateInConnected);
                Assert.True(state.TokenStateInDisconnected);
            }
        }

        [Fact]
        public async Task AbortFromHubMethodForcesClientDisconnect()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<AbortHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.InvokeAsync(nameof(AbortHub.Kill));

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task ObservableHubRemovesSubscriptionsWithInfiniteStreams()
        {
            var observable = new Observable<int>();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(s => s.AddSingleton(observable));
            var endPoint = serviceProvider.GetService<HubEndPoint<ObservableHub>>();

            var waitForSubscribe = new TaskCompletionSource<object>();
            observable.OnSubscribe = o =>
            {
                waitForSubscribe.TrySetResult(null);
            };

            var waitForDispose = new TaskCompletionSource<object>();
            observable.OnDispose = o =>
            {
                waitForDispose.TrySetResult(null);
            };

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                async Task Produce()
                {
                    int i = 0;
                    while (true)
                    {
                        observable.OnNext(i++);
                        await Task.Delay(100);
                    }
                }

                _ = Produce();

                Assert.Empty(observable.Observers);

                var subscribeTask = client.StreamAsync(nameof(ObservableHub.Subscribe));

                await waitForSubscribe.Task.OrTimeout();

                Assert.Single(observable.Observers);

                client.Dispose();

                // We don't care if this throws, we just expect it to complete
                try
                {
                    await subscribeTask.OrTimeout();
                }
                catch
                {

                }

                await waitForDispose.Task.OrTimeout();

                Assert.Empty(observable.Observers);

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task ObservableHubRemovesSubscriptions()
        {
            var observable = new Observable<int>();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(s => s.AddSingleton(observable));
            var endPoint = serviceProvider.GetService<HubEndPoint<ObservableHub>>();

            var waitForSubscribe = new TaskCompletionSource<object>();
            observable.OnSubscribe = o =>
            {
                waitForSubscribe.TrySetResult(null);
            };

            var waitForDispose = new TaskCompletionSource<object>();
            observable.OnDispose = o =>
            {
                waitForDispose.TrySetResult(null);
            };

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                async Task Subscribe()
                {
                    var results = await client.StreamAsync(nameof(ObservableHub.Subscribe));

                    var items = results.OfType<StreamItemMessage>().ToList();

                    Assert.Single(items);
                    Assert.Equal(2, (long)items[0].Item);
                }

                observable.OnNext(1);

                Assert.Empty(observable.Observers);

                var subscribeTask = Subscribe();

                await waitForSubscribe.Task.OrTimeout();

                Assert.Single(observable.Observers);

                observable.OnNext(2);

                observable.Complete();

                await subscribeTask.OrTimeout();

                client.Dispose();

                await waitForDispose.Task.OrTimeout();

                Assert.Empty(observable.Observers);

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task ObservableHubRemovesSubscriptionWhenCanceledFromClient()
        {
            var observable = new Observable<int>();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(s => s.AddSingleton(observable));
            var endPoint = serviceProvider.GetService<HubEndPoint<ObservableHub>>();

            var waitForSubscribe = new TaskCompletionSource<object>();
            observable.OnSubscribe = o =>
            {
                waitForSubscribe.TrySetResult(null);
            };

            var waitForDispose = new TaskCompletionSource<object>();
            observable.OnDispose = o =>
            {
                waitForDispose.TrySetResult(null);
            };

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var invocationId = await client.SendStreamInvocationAsync(nameof(ObservableHub.Subscribe)).OrTimeout();

                await waitForSubscribe.Task.OrTimeout();

                await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).OrTimeout();

                await waitForDispose.Task.OrTimeout();

                var message = await client.ReadAsync().OrTimeout();

                Assert.IsType<CompletionMessage>(message);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task MissingHandshakeAndMessageSentFromHubConnectionCanBeDisposedCleanly()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<SimpleHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint, false, false);

                // kill the connection
                client.Dispose();

                await endPointTask;

                Assert.Null(client.HandshakeResponseMessage);
            }
        }

        [Fact]
        public async Task HandshakeTimesOut()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services.Configure<HubOptions>(options =>
                {
                    options.HandshakeTimeout = TimeSpan.FromMilliseconds(5);
                });
            });
            var endPoint = serviceProvider.GetService<HubEndPoint<SimpleHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint, false);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("Handshake was canceled.", client.HandshakeResponseMessage.Error);

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanLoadHubContext()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<IHubContext<SimpleHub>>();
            await context.Clients.All.SendAsync("Send", "test");
        }

        [Fact]
        public async Task CanLoadTypedHubContext()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<IHubContext<SimpleTypedHub, ITypedHubClient>>();
            await context.Clients.All.Send("test");
        }

        [Fact]
        public async Task HandshakeFailureFromUnknownProtocolSendsResponseWithError()
        {
            var hubProtocolMock = new Mock<IHubProtocol>();
            hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

            var endPoint = HubEndPointTestUtils.GetHubEndpoint(typeof(HubT));

            using (var client = new TestClient(protocol: hubProtocolMock.Object))
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("The protocol 'CustomProtocol' is not supported.", client.HandshakeResponseMessage.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HandshakeFailureFromUnsupportedFormatSendsResponseWithError()
        {
            var hubProtocolMock = new Mock<IHubProtocol>();
            hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

            var endPoint = HubEndPointTestUtils.GetHubEndpoint(typeof(HubT));

            using (var client = new TestClient(protocol: new MessagePackHubProtocol()))
            {
                client.Connection.SupportedFormats = TransferFormat.Text;

                var endPointTask = await client.ConnectAsync(endPoint);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("Cannot use the 'messagepack' protocol on the current transport. The transport does not support 'Binary' transfer format.", client.HandshakeResponseMessage.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HandshakeSuccessSendsResponseWithoutError()
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(typeof(HubT));

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Null(client.HandshakeResponseMessage.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfLifetimeManagerOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<Hub>>();
            mockLifetimeManager
                .Setup(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()))
                .Throws(new InvalidOperationException("Lifetime manager OnConnectedAsync failed."));
            var mockHubActivator = new Mock<IHubActivator<Hub>>();

            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
                services.AddSingleton(mockHubActivator.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<Hub>>();

            using (var client = new TestClient())
            {
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () =>
                        {
                            var endPointTask = await client.ConnectAsync(endPoint);
                            await endPointTask.OrTimeout();
                        });
                Assert.Equal("Lifetime manager OnConnectedAsync failed.", exception.Message);

                client.Dispose();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                // No hubs should be created since the connection is terminated
                mockHubActivator.Verify(m => m.Create(), Times.Never);
                mockHubActivator.Verify(m => m.Release(It.IsAny<Hub>()), Times.Never);
            }
        }

        [Fact]
        public async Task HubOnDisconnectedAsyncCalledIfHubOnConnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<OnConnectedThrowsHub>>();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<OnConnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);
                client.Dispose();

                await endPointTask.OrTimeout();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfHubOnDisconnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<OnDisconnectedThrowsHub>>();
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<OnDisconnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);
                client.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endPointTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValueFromTask()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var result = (await client.InvokeAsync(nameof(MethodHub.TaskValueMethod)).OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(42L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubMethodsAreCaseInsensitive(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var client = new TestClient())
            {
                var endPointTask = (Task)await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var message = await client.InvokeAsync(methodName).OrTimeout();

                Assert.Equal($"An unexpected error occurred invoking '{methodName}' on the server. InvalidOperationException: BOOM!", message.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodDoesNotSendResultWhenInvocationIsNonBlocking()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.SendInvocationAsync(nameof(MethodHub.ValueMethod), nonBlocking: true).OrTimeout();

                // kill the connection
                client.Dispose();

                // Ensure the client channel is empty
                Assert.Null(client.TryRead());

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanBeVoid()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var result = (await client.InvokeAsync(nameof(MethodHub.VoidMethod)).OrTimeout()).Result;

                Assert.Null(result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanBeRenamedWithAttribute()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var result = (await client.InvokeAsync("RenamedMethod").OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(43L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodNameAttributeIsInherited()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<InheritedHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var result = (await client.InvokeAsync("RenamedVirtualMethod").OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(34L, result);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Theory]
        [InlineData(nameof(MethodHub.VoidMethod))]
        [InlineData(nameof(MethodHub.MethodThatThrows))]
        [InlineData(nameof(MethodHub.ValueMethod))]
        public async Task NonBlockingInvocationDoesNotSendCompletion(string methodName)
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient(synchronousCallbacks: true))
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                // This invocation should be completely synchronous
                await client.SendInvocationAsync(methodName, nonBlocking: true).OrTimeout();

                // kill the connection
                client.Dispose();

                // only thing written should be close message
                var closeMessage = await client.ReadAsync().OrTimeout();
                Assert.IsType<CloseMessage>(closeMessage);

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodWithMultiParam()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<InheritedHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<InheritedHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var result = await client.InvokeAsync(nameof(MethodHub.Dispose)).OrTimeout();

                Assert.Equal("Unknown hub method 'Dispose'", result.Error);

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task BroadcastHubMethodSendsToAllClients(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.BroadcastMethod), "test").OrTimeout();

                foreach (var result in await Task.WhenAll(
                    firstClient.ReadAsync(),
                    secondClient.ReadAsync()).OrTimeout())
                {
                    var invocation = Assert.IsType<InvocationMessage>(result);
                    Assert.Equal("Broadcast", invocation.Target);
                    Assert.Single(invocation.Arguments);
                    Assert.Equal("test", invocation.Arguments[0]);
                }

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Fact]
        public async Task SendArraySendsArrayToAllClients()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.SendArray)).OrTimeout();

                foreach (var result in await Task.WhenAll(
                    firstClient.ReadAsync(),
                    secondClient.ReadAsync()).OrTimeout())
                {
                    var invocation = Assert.IsType<InvocationMessage>(result);
                    Assert.Equal("Array", invocation.Target);
                    Assert.Single(invocation.Arguments);
                    var values = ((JArray)invocation.Arguments[0]).Select(t => t.Value<int>()).ToArray();
                    Assert.Equal(new int[] { 1, 2, 3 }, values);
                }

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthers(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync("SendToOthers", "To others").OrTimeout();

                var secondClientResult = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To others", invocation.Arguments[0]);

                var firstClientResult = await firstClient.ReadAsync().OrTimeout();
                var completion = Assert.IsType<CompletionMessage>(firstClientResult);

                await secondClient.SendInvocationAsync("BroadcastMethod", "To everyone").OrTimeout();
                firstClientResult = await firstClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(firstClientResult);
                Assert.Equal("Broadcast", invocation.Target);
                Assert.Equal("To everyone", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();


            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToCaller(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync("SendToCaller", "To caller").OrTimeout();

                var firstClientResult = await firstClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(firstClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To caller", invocation.Arguments[0]);

                await firstClient.SendInvocationAsync("BroadcastMethod", "To everyone").OrTimeout();
                var secondClientResult = await secondClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Broadcast", invocation.Target);
                Assert.Equal("To everyone", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToAllExcept(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            using (var thirdClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);
                Task thirdEndPointTask = await thirdClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).OrTimeout();

                var excludeSecondClientId = new HashSet<string>();
                excludeSecondClientId.Add(secondClient.Connection.ConnectionId);
                var excludeThirdClientId = new HashSet<string>();
                excludeThirdClientId.Add(thirdClient.Connection.ConnectionId);

                await firstClient.SendInvocationAsync("SendToAllExcept", "To second", excludeThirdClientId).OrTimeout();
                await firstClient.SendInvocationAsync("SendToAllExcept", "To third", excludeSecondClientId).OrTimeout();

                var secondClientResult = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To second", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To third", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask, thirdEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleClients(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            using (var thirdClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);
                Task thirdEndPointTask = await thirdClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).OrTimeout();

                var secondAndThirdClients = new HashSet<string> {secondClient.Connection.ConnectionId,
                    thirdClient.Connection.ConnectionId };

                await firstClient.SendInvocationAsync("SendToMultipleClients", "Second and Third", secondAndThirdClients).OrTimeout();

                var secondClientResult = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                // Check that first client only got the completion message
                var hubMessage = await firstClient.ReadAsync().OrTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);
                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask, thirdEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleUsers(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient(addClaimId: true))
            using (var secondClient = new TestClient(addClaimId: true))
            using (var thirdClient = new TestClient(addClaimId: true))
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);
                Task thirdEndPointTask = await thirdClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).OrTimeout();

                var secondAndThirdClients = new HashSet<string> {secondClient.Connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    thirdClient.Connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value };

                await firstClient.SendInvocationAsync(nameof(MethodHub.SendToMultipleUsers), secondAndThirdClients, "Second and Third").OrTimeout();

                var secondClientResult = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                // Check that first client only got the completion message
                var hubMessage = await firstClient.ReadAsync().OrTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);
                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask, thirdEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanAddAndSendToGroup(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").OrTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                result = (await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout()).Result;

                await firstClient.SendInvocationAsync(nameof(MethodHub.GroupSendMethod), "testGroup", "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToGroupExcept(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").OrTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();
                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();

                var excludedIds = new List<string> { firstClient.Connection.ConnectionId };

                await firstClient.SendInvocationAsync("GroupExceptSendMethod", "testGroup", "test", excludedIds).OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // Check that first client only got the completion message
                hubMessage = await firstClient.ReadAsync().OrTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);

                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthersInGroup(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").OrTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();
                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();

                await firstClient.SendInvocationAsync("SendToOthersInGroup", "testGroup", "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // Check that first client only got the completion message
                hubMessage = await firstClient.ReadAsync().OrTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);

                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task InvokeMultipleGroups(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "GroupA").OrTimeout();
                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "GroupB").OrTimeout(); ;

                var groupNames = new List<string> { "GroupA", "GroupB" };
                await firstClient.SendInvocationAsync(nameof(MethodHub.SendToMultipleGroups), "test", groupNames).OrTimeout();

                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                hubMessage = await firstClient.ReadAsync().OrTimeout();
                invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
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
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.SendInvocationAsync(nameof(MethodHub.GroupRemoveMethod), "testGroup").OrTimeout();

                // kill the connection
                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToUser(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient(addClaimId: true))
            using (var secondClient = new TestClient(addClaimId: true))
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync("ClientSendMethod", secondClient.Connection.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToConnection(Type hubType)
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync("ConnectionSendMethod", secondClient.Connection.ConnectionId, "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Fact]
        public async Task DelayedSendTest()
        {
            var endPoint = HubEndPointTestUtils.GetHubEndpoint(typeof(HubT));

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                Task firstEndPointTask = await firstClient.ConnectAsync(endPoint);
                Task secondEndPointTask = await secondClient.ConnectAsync(endPoint);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                await firstClient.SendInvocationAsync("DelayedSend", secondClient.Connection.ConnectionId, "test").OrTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().OrTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstEndPointTask, secondEndPointTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(StreamingMethodAndHubProtocols))]
        public async Task HubsCanStreamResponses(string method, IHubProtocol protocol)
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<StreamingHub>>();
            var invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetReturnType(It.IsAny<string>())).Returns(typeof(string));

            using (var client = new TestClient(synchronousCallbacks: false, protocol: protocol, invocationBinder: invocationBinder.Object))
            {
                client.Connection.SupportedFormats = protocol.TransferFormat;

                var endPointTask = await client.ConnectAsync(endPoint);

                // Wait for a connection, or for the endpoint to fail.
                await client.Connected.OrThrowIfOtherFails(endPointTask).OrTimeout();

                var messages = await client.StreamAsync(method, 4).OrTimeout();

                Assert.Equal(5, messages.Count);
                HubEndPointTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "0"), messages[0]);
                HubEndPointTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "1"), messages[1]);
                HubEndPointTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "2"), messages[2]);
                HubEndPointTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "3"), messages[3]);
                HubEndPointTestUtils.AssertHubMessage(CompletionMessage.Empty(string.Empty), messages[4]);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task NonErrorCompletionSentWhenStreamCanceledFromClient()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<StreamingHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var invocationId = Guid.NewGuid().ToString("N");
                await client.SendHubMessageAsync(new StreamInvocationMessage(invocationId, nameof(StreamingHub.BlockingStream),
                    argumentBindingException: null));

                // cancel the Streaming method
                await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).OrTimeout();

                var hubMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().OrTimeout());
                Assert.Equal(invocationId, hubMessage.InvocationId);
                Assert.Null(hubMessage.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task ReceiveCorrectErrorFromStreamThrowing()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<StreamingHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var messages = await client.StreamAsync(nameof(StreamingHub.ThrowStream));

                Assert.Equal(1, messages.Count);
                var completion = messages[0] as CompletionMessage;
                Assert.NotNull(completion);
                Assert.Equal("Exception from observable", completion.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanSendToConnectionsWithDifferentProtocols()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client1 = new TestClient(protocol: new JsonHubProtocol()))
            using (var client2 = new TestClient(protocol: new MessagePackHubProtocol()))
            {
                Task firstEndPointTask = await client1.ConnectAsync(endPoint);
                Task secondEndPointTask = await client2.ConnectAsync(endPoint);

                await client1.Connected.OrTimeout();
                await client2.Connected.OrTimeout();

                var sentMessage = "From Json";

                await client1.SendInvocationAsync(nameof(MethodHub.BroadcastMethod), sentMessage);
                var message1 = await client1.ReadAsync().OrTimeout();
                var message2 = await client2.ReadAsync().OrTimeout();

                var completion1 = message1 as InvocationMessage;
                Assert.NotNull(completion1);
                Assert.Equal(sentMessage, completion1.Arguments[0]);
                var completion2 = message2 as InvocationMessage;
                Assert.NotNull(completion2);
                // Argument[0] is a 'MsgPackObject' with a string internally, ToString to compare it
                Assert.Equal(sentMessage, completion2.Arguments[0].ToString());

                client1.Dispose();
                client2.Dispose();

                await firstEndPointTask.OrTimeout();
                await secondEndPointTask.OrTimeout();
            }
        }

        public static IEnumerable<object[]> StreamingMethodAndHubProtocols
        {
            get
            {
                foreach (var method in new[]
                {
                    nameof(StreamingHub.CounterChannel), nameof(StreamingHub.CounterChannelAsync), nameof(StreamingHub.CounterChannelValueTaskAsync),
                    nameof(StreamingHub.CounterObservable), nameof(StreamingHub.CounterObservableAsync), nameof(StreamingHub.CounterObservableValueTaskAsync)
                })
                {
                    foreach (var protocol in new IHubProtocol[] { new JsonHubProtocol(), new MessagePackHubProtocol() })
                    {
                        yield return new object[] { method, protocol };
                    }
                }
            }
        }

        [Fact]
        public async Task UnauthorizedConnectionCannotInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
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
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.NotNull(message.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task AuthorizedConnectionCanInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
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
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubOptionsCanUseCustomJsonSerializerSettings()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services
                    .AddSignalR()
                    .AddJsonProtocol(o =>
                    {
                        o.PayloadSerializerSettings = new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver()
                        };
                    });
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = (InvocationMessage)await client.ReadAsync().OrTimeout();

                var customItem = message.Arguments[0].ToString();
                // by default properties serialized by JsonHubProtocol are using camelCasing
                Assert.Contains("Message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task JsonHubProtocolUsesCamelCasingByDefault()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = (InvocationMessage)await client.ReadAsync().OrTimeout();

                var customItem = message.Arguments[0].ToString();
                // originally Message, paramName
                Assert.Contains("message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubOptionsCanUseCustomMessagePackSettings()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR()
                    .AddMessagePackProtocol(options =>
                    {
                        options.SerializationContext.SerializationMethod = SerializationMethod.Array;
                    });
            });

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            var msgPackOptions = serviceProvider.GetRequiredService<IOptions<MessagePackHubProtocolOptions>>();
            using (var client = new TestClient(synchronousCallbacks: false, protocol: new MessagePackHubProtocol(msgPackOptions)))
            {
                client.Connection.SupportedFormats = TransferFormat.Binary;
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout());

                var msgPackObject = Assert.IsType<MessagePackObject>(message.Arguments[0]);
                // Custom serialization - object was serialized as an array and not a map
                Assert.True(msgPackObject.IsArray);
                Assert.Equal(new[] { "test", "param" }, ((MessagePackObject[])msgPackObject.ToObject()).Select(o => o.AsString()));

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanGetHttpContextFromHubConnectionContext()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var httpContext = new DefaultHttpContext();
                client.Connection.SetHttpContext(httpContext);
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).OrTimeout()).Result;
                Assert.True((bool)result);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task GetHttpContextFromHubConnectionContextHandlesNull()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();

            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient())
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).OrTimeout()).Result;
                Assert.False((bool)result);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task AcceptsPingMessages()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient(false, new JsonHubProtocol()))
            {
                var endPointTask = await client.ConnectAsync(endPoint);
                await client.Connected.OrTimeout();

                // Send a ping
                await client.SendHubMessageAsync(PingMessage.Instance).OrTimeout();

                // Now do an invocation to make sure we processed the ping message
                var completion = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).OrTimeout();
                Assert.NotNull(completion);

                client.Dispose();

                await endPointTask.OrTimeout();
            }
        }

        [Fact]
        public async Task DoesNotWritePingMessagesIfSufficientOtherMessagesAreSent()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)));
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient(false, new JsonHubProtocol()))
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                // Echo a bunch of stuff, waiting 10ms between each, until 500ms have elapsed
                DateTime start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalMilliseconds <= 500.0)
                {
                    await client.SendInvocationAsync("Echo", "foo").OrTimeout();
                    await Task.Delay(10);
                }

                // Shut down
                client.Dispose();

                await endPointTask.OrTimeout();

                client.Connection.Transport.Output.Complete();

                // We shouldn't have any ping messages
                HubMessage message;
                var counter = 0;
                while ((message = await client.ReadAsync()) != null)
                {
                    counter += 1;
                    Assert.IsNotType<PingMessage>(message);
                }
                Assert.InRange(counter, 1, 50);
            }
        }

        [Fact]
        public async Task WritesPingMessageIfNothingWrittenWhenKeepAliveIntervalElapses()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)));
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient(false, new JsonHubProtocol()))
            {
                var endPointTask = await client.ConnectAsync(endPoint);
                await client.Connected.OrTimeout();

                // Wait 500 ms, but make sure to yield some time up to unblock concurrent threads
                // This is useful on AppVeyor because it's slow enough to end up with no time
                // being available for the endpoint to run.
                for (var i = 0; i < 50; i += 1)
                {
                    client.Connection.TickHeartbeat();
                    await Task.Yield();
                    await Task.Delay(10);
                }

                // Shut down
                client.Dispose();

                await endPointTask.OrTimeout();

                client.Connection.Transport.Output.Complete();

                // We should have all pings (and close message)
                HubMessage message;
                var pingCounter = 0;
                var hasCloseMessage = false;
                while ((message = await client.ReadAsync().OrTimeout()) != null)
                {
                    if (hasCloseMessage)
                    {
                        Assert.True(false, "Received message after close");
                    }

                    switch (message)
                    {
                        case PingMessage _:
                            pingCounter += 1;
                            break;
                        case CloseMessage _:
                            hasCloseMessage = true;
                            break;
                        default:
                            Assert.True(false, "Unexpected message type: " + message.GetType().Name);
                            break;
                    }
                }
                Assert.InRange(pingCounter, 1, Int32.MaxValue);
            }
        }

        [Fact]
        public async Task EndingConnectionSendsCloseMessageWithNoError()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<MethodHub>>();

            using (var client = new TestClient(false, new JsonHubProtocol()))
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                await client.Connected.OrTimeout();

                // Shut down
                client.Dispose();

                await endPointTask.OrTimeout();

                client.Connection.Transport.Output.Complete();

                var message = await client.ReadAsync().OrTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(message);
                Assert.Null(closeMessage.Error);
            }
        }

        [Fact]
        public async Task ErrorInHubOnConnectSendsCloseMessageWithError()
        {
            var serviceProvider = HubEndPointTestUtils.CreateServiceProvider();
            var endPoint = serviceProvider.GetService<HubEndPoint<OnConnectedThrowsHub>>();

            using (var client = new TestClient(false, new JsonHubProtocol()))
            {
                var endPointTask = await client.ConnectAsync(endPoint);

                var message = await client.ReadAsync().OrTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(message);
                Assert.Equal("Connection closed with an error. InvalidOperationException: Hub OnConnected failed.", closeMessage.Error);

                await endPointTask.OrTimeout();
            }
        }

        public static IEnumerable<object[]> HubTypes()
        {
            yield return new[] { typeof(DynamicTestHub) };
            yield return new[] { typeof(MethodHub) };
            yield return new[] { typeof(HubT) };
        }
    }
}

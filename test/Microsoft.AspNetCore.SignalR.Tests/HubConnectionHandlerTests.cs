// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubConnectionHandlerTests
    {
        [Fact]
        public async Task HubsAreDisposed()
        {
            var trackDispose = new TrackDispose();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(trackDispose));
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<DisposeTrackingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask;

                Assert.Equal(2, trackDispose.DisposeCount);
            }
        }

        [Fact]
        public async Task ConnectionAbortedTokenTriggers()
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state));
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();

                Assert.True(state.TokenCallbackTriggered);
                Assert.False(state.TokenStateInConnected);
                Assert.True(state.TokenStateInDisconnected);
            }
        }

        [Fact]
        public async Task AbortFromHubMethodForcesClientDisconnect()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AbortHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.InvokeAsync(nameof(AbortHub.Kill));

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task MissingHandshakeAndMessageSentFromHubConnectionCanBeDisposedCleanly()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<SimpleHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler, false, false);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask;

                Assert.Null(client.HandshakeResponseMessage);
            }
        }

        [Fact]
        public async Task HandshakeTimesOut()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.Configure<HubOptions>(options =>
                {
                    options.HandshakeTimeout = TimeSpan.FromMilliseconds(5);
                });
            });
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<SimpleHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler, false);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("Handshake was canceled.", client.HandshakeResponseMessage.Error);

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanLoadHubContext()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<IHubContext<SimpleHub>>();
            await context.Clients.All.SendAsync("Send", "test");
        }

        [Fact]
        public async Task CanLoadTypedHubContext()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<IHubContext<SimpleTypedHub, ITypedHubClient>>();
            await context.Clients.All.Send("test");
        }

        [Fact]
        public async Task HandshakeFailureFromUnknownProtocolSendsResponseWithError()
        {
            var hubProtocolMock = new Mock<IHubProtocol>();
            hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));

            using (var client = new TestClient(protocol: hubProtocolMock.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("The protocol 'CustomProtocol' is not supported.", client.HandshakeResponseMessage.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HandshakeFailureFromUnsupportedFormatSendsResponseWithError()
        {
            var hubProtocolMock = new Mock<IHubProtocol>();
            hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));

            using (var client = new TestClient(protocol: new MessagePackHubProtocol()))
            {
                client.SupportedFormats = TransferFormat.Text;

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("Cannot use the 'messagepack' protocol on the current transport. The transport does not support 'Binary' transfer format.", client.HandshakeResponseMessage.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task SendingHandshakeRequestInChunksWorks()
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));
            var part1 = Encoding.UTF8.GetBytes("{\"protocol\": \"json\"");
            var part2 = Encoding.UTF8.GetBytes(",\"version\": 1}");
            var part3 = Encoding.UTF8.GetBytes("\u001e");

            using (var client = new TestClient())
            {
                client.SupportedFormats = TransferFormat.Text;

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler,
                                                        sendHandshakeRequestMessage: false,
                                                        expectedHandshakeResponseMessage: false);

                // Wait for the handshake response
                var task = client.ReadAsync(isHandshake: true);

                await client.Connection.Application.Output.WriteAsync(part1);

                Assert.False(task.IsCompleted);

                await client.Connection.Application.Output.WriteAsync(part2);

                Assert.False(task.IsCompleted);

                await client.Connection.Application.Output.WriteAsync(part3);

                Assert.True(task.IsCompleted);

                var response = (await task) as HandshakeResponseMessage;
                Assert.NotNull(response);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task SendingInvocatonInChunksWorks()
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));
            var part1 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"1\", ");
            var part2 = Encoding.UTF8.GetBytes("\"target\": \"Echo\", \"arguments\"");
            var part3 = Encoding.UTF8.GetBytes(":[\"hello\"]}\u001e");

            using (var client = new TestClient())
            {
                client.SupportedFormats = TransferFormat.Text;

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Wait for the hub completion
                var task = client.ReadAsync();

                await client.Connection.Application.Output.WriteAsync(part1);

                Assert.False(task.IsCompleted);

                await client.Connection.Application.Output.WriteAsync(part2);

                Assert.False(task.IsCompleted);

                await client.Connection.Application.Output.WriteAsync(part3);

                Assert.True(task.IsCompleted);

                var completionMessage = await task as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("hello", completionMessage.Result);
                Assert.Equal("1", completionMessage.InvocationId);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task SendingHandshakeRequestAndInvocationInSamePayloadParsesHandshakeAndInvocation()
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));
            var payload = Encoding.UTF8.GetBytes("{\"protocol\": \"json\",\"version\": 1}\u001e{\"type\":1, \"invocationId\":\"1\", \"target\": \"Echo\", \"arguments\":[\"hello\"]}\u001e");

            using (var client = new TestClient())
            {
                client.SupportedFormats = TransferFormat.Text;

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler,
                                                        sendHandshakeRequestMessage: false,
                                                        expectedHandshakeResponseMessage: false);

                // Wait for the handshake response
                var task = client.ReadAsync(isHandshake: true);

                await client.Connection.Application.Output.WriteAsync(payload);

                Assert.True(task.IsCompleted);

                var response = await task as HandshakeResponseMessage;
                Assert.NotNull(response);

                var completionMessage = await client.ReadAsync() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("hello", completionMessage.Result);
                Assert.Equal("1", completionMessage.InvocationId);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HandshakeSuccessSendsResponseWithoutError()
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Null(client.HandshakeResponseMessage.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HandshakeFailureFromIncompatibleProtocolVersionSendsResponseWithError()
        {
            var hubProtocolMock = new Mock<IHubProtocol>();
            hubProtocolMock.Setup(m => m.Name).Returns("json");
            hubProtocolMock.Setup(m => m.Version).Returns(9001);

            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));

            using (var client = new TestClient(protocol: hubProtocolMock.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                Assert.NotNull(client.HandshakeResponseMessage);
                Assert.Equal("The server does not support version 9001 of the 'json' protocol.", client.HandshakeResponseMessage.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
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

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
                services.AddSingleton(mockHubActivator.Object);
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<Hub>>();

            using (var client = new TestClient())
            {
                var exception =
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () =>
                        {
                            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                            await connectionHandlerTask.OrTimeout();
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
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                client.Dispose();

                await connectionHandlerTask.OrTimeout();

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfHubOnDisconnectedAsyncThrows()
        {
            var mockLifetimeManager = new Mock<HubLifetimeManager<OnDisconnectedThrowsHub>>();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(mockLifetimeManager.Object);
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnDisconnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                client.Dispose();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await connectionHandlerTask);
                Assert.Equal("Hub OnDisconnected failed.", exception.Message);

                mockLifetimeManager.Verify(m => m.OnConnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
                mockLifetimeManager.Verify(m => m.OnDisconnectedAsync(It.IsAny<HubConnectionContext>()), Times.Once);
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValueFromTask()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync(nameof(MethodHub.TaskValueMethod)).OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(42L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubMethodsAreCaseInsensitive(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var client = new TestClient())
            {
                var connectionHandlerTask = (Task)await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync("echo", "hello").OrTimeout()).Result;

                Assert.Equal("hello", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [InlineData(nameof(MethodHub.MethodThatThrows), true)]
        [InlineData(nameof(MethodHub.MethodThatYieldsFailedTask), false)]
        public async Task HubMethodCanThrowOrYieldFailedTask(string methodName, bool detailedErrors)
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = detailedErrors;
                });
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var message = await client.InvokeAsync(methodName).OrTimeout();

                if (detailedErrors)
                {
                    Assert.Equal($"An unexpected error occurred invoking '{methodName}' on the server. InvalidOperationException: BOOM!", message.Error);
                }
                else
                {
                    Assert.Equal($"An unexpected error occurred invoking '{methodName}' on the server.", message.Error);
                }

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodDoesNotSendResultWhenInvocationIsNonBlocking()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.SendInvocationAsync(nameof(MethodHub.ValueMethod), nonBlocking: true).OrTimeout();

                // kill the connection
                client.Dispose();

                // Ensure the client channel is empty
                var message = client.TryRead();
                switch (message)
                {
                    case CloseMessage close:
                        break;
                    default:
                        Assert.Null(message);
                        break;
                }

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanBeVoid()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync(nameof(MethodHub.VoidMethod)).OrTimeout()).Result;

                Assert.Null(result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodCanBeRenamedWithAttribute()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync("RenamedMethod").OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(43L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodNameAttributeIsInherited()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<InheritedHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync("RenamedVirtualMethod").OrTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(34L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [InlineData(nameof(MethodHub.VoidMethod))]
        [InlineData(nameof(MethodHub.MethodThatThrows))]
        [InlineData(nameof(MethodHub.ValueMethod))]
        public async Task NonBlockingInvocationDoesNotSendCompletion(string methodName)
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // This invocation should be completely synchronous
                await client.SendInvocationAsync(methodName, nonBlocking: true).OrTimeout();

                // kill the connection
                client.Dispose();

                // only thing written should be close message
                var closeMessage = await client.ReadAsync().OrTimeout();
                Assert.IsType<CloseMessage>(closeMessage);

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubMethodWithMultiParam()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync(nameof(MethodHub.ConcatString), (byte)32, 42, 'm', "string").OrTimeout()).Result;

                Assert.Equal("32, 42, m, string", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanCallInheritedHubMethodFromInheritingHub()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<InheritedHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync(nameof(InheritedHub.BaseMethod), "string").OrTimeout()).Result;

                Assert.Equal("string", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanCallOverridenVirtualHubMethod()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<InheritedHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = (await client.InvokeAsync(nameof(InheritedHub.VirtualMethod), 10).OrTimeout()).Result;

                Assert.Equal(0L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallOverriddenBaseHubMethod()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = await client.InvokeAsync(nameof(MethodHub.OnDisconnectedAsync)).OrTimeout();

                Assert.Equal("Unknown hub method 'OnDisconnectedAsync'", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public void HubsCannotHaveOverloadedMethods()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            try
            {
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<InvalidHub>>();
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
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = await client.InvokeAsync(nameof(MethodHub.StaticMethod)).OrTimeout();

                Assert.Equal("Unknown hub method 'StaticMethod'", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallObjectMethodsOnHub()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

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

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CannotCallDisposeMethodOnHub()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var result = await client.InvokeAsync(nameof(MethodHub.Dispose)).OrTimeout();

                Assert.Equal("Unknown hub method 'Dispose'", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task BroadcastHubMethodSendsToAllClients(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Fact]
        public async Task SendArraySendsArrayToAllClients()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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
                    Assert.Equal(new[] { 1, 2, 3 }, values);
                }

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthers(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();


            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToCaller(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToAllExcept(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            using (var thirdClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);
                var thirdConnectionHandlerTask = await thirdClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleClients(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            using (var thirdClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);
                var thirdConnectionHandlerTask = await thirdClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleUsers(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient(addClaimId: true))
            using (var secondClient = new TestClient(addClaimId: true))
            using (var thirdClient = new TestClient(addClaimId: true))
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);
                var thirdConnectionHandlerTask = await thirdClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanAddAndSendToGroup(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToGroupExcept(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").OrTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();
                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").OrTimeout();

                var excludedConnectionIds = new List<string> { firstClient.Connection.ConnectionId };

                await firstClient.SendInvocationAsync("GroupExceptSendMethod", "testGroup", "test", excludedConnectionIds).OrTimeout();

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthersInGroup(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task InvokeMultipleGroups(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Fact]
        public async Task RemoveFromGroupWhenNotInGroupDoesNotFail()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.SendInvocationAsync(nameof(MethodHub.GroupRemoveMethod), "testGroup").OrTimeout();

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToUser(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient(addClaimId: true))
            using (var secondClient = new TestClient(addClaimId: true))
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToConnection(Type hubType)
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType);

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Fact]
        public async Task DelayedSendTest()
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT));

            using (var firstClient = new TestClient())
            using (var secondClient = new TestClient())
            {
                var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(StreamingMethodAndHubProtocols))]
        public async Task HubsCanStreamResponses(string method, string protocolName)
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
            var invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetReturnType(It.IsAny<string>())).Returns(typeof(string));

            using (var client = new TestClient(protocol: protocol, invocationBinder: invocationBinder.Object))
            {
                client.SupportedFormats = protocol.TransferFormat;

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Wait for a connection, or for the endpoint to fail.
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                var messages = await client.StreamAsync(method, 4).OrTimeout();

                Assert.Equal(5, messages.Count);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "0"), messages[0]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "1"), messages[1]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "2"), messages[2]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "3"), messages[3]);
                HubConnectionHandlerTestUtils.AssertHubMessage(CompletionMessage.Empty(string.Empty), messages[4]);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task NonErrorCompletionSentWhenStreamCanceledFromClient()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var invocationId = Guid.NewGuid().ToString("N");
                await client.SendHubMessageAsync(new StreamInvocationMessage(invocationId, nameof(StreamingHub.BlockingStream), null, Array.Empty<object>()));

                // cancel the Streaming method
                await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).OrTimeout();

                var hubMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().OrTimeout());
                Assert.Equal(invocationId, hubMessage.InvocationId);
                Assert.Null(hubMessage.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReceiveCorrectErrorFromStreamThrowing(bool detailedErrors)
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                builder.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = detailedErrors;
                }));
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var messages = await client.StreamAsync(nameof(StreamingHub.ThrowStream));

                Assert.Equal(1, messages.Count);
                var completion = messages[0] as CompletionMessage;
                Assert.NotNull(completion);
                if (detailedErrors)
                {
                    Assert.Equal("An error occurred on the server while streaming results. Exception: Exception from channel", completion.Error);
                }
                else
                {
                    Assert.Equal("An error occurred on the server while streaming results.", completion.Error);
                }

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task CanSendToConnectionsWithDifferentProtocols()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client1 = new TestClient(protocol: new JsonHubProtocol()))
            using (var client2 = new TestClient(protocol: new MessagePackHubProtocol()))
            {
                var firstConnectionHandlerTask = await client1.ConnectAsync(connectionHandler);
                var secondConnectionHandlerTask = await client2.ConnectAsync(connectionHandler);

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

                await firstConnectionHandlerTask.OrTimeout();
                await secondConnectionHandlerTask.OrTimeout();
            }
        }

        public static IEnumerable<object[]> StreamingMethodAndHubProtocols
        {
            get
            {
                foreach (var method in new[]
                {
                    nameof(StreamingHub.CounterChannel), nameof(StreamingHub.CounterChannelAsync), nameof(StreamingHub.CounterChannelValueTaskAsync)
                })
                {
                    foreach (var protocolName in HubProtocolHelpers.AllProtocolNames)
                    {
                        yield return new object[] { method, protocolName };
                    }
                }
            }
        }

        [Fact]
        public async Task UnauthorizedConnectionCannotInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
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

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.NotNull(message.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task AuthorizedConnectionCanInvokeHubMethodWithAuthorization()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
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

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                client.Connection.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).OrTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubOptionsCanUseCustomJsonSerializerSettings()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
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

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = (InvocationMessage)await client.ReadAsync().OrTimeout();

                var customItem = message.Arguments[0].ToString();
                // by default properties serialized by JsonHubProtocol are using camelCasing
                Assert.Contains("Message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task JsonHubProtocolUsesCamelCasingByDefault()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = (InvocationMessage)await client.ReadAsync().OrTimeout();

                var customItem = message.Arguments[0].ToString();
                // originally Message, paramName
                Assert.Contains("message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task HubOptionsCanUseCustomMessagePackSettings()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR()
                    .AddMessagePackProtocol(options =>
                    {
                        options.FormatterResolvers.Insert(0, new CustomFormatter());
                    });
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            var msgPackOptions = serviceProvider.GetRequiredService<IOptions<MessagePackHubProtocolOptions>>();
            using (var client = new TestClient(protocol: new MessagePackHubProtocol(msgPackOptions)))
            {
                client.SupportedFormats = TransferFormat.Binary;
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).OrTimeout();

                var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout());

                var result = message.Arguments[0] as Dictionary<object, object>;
                Assert.Equal("formattedString", result["Message"]);
                Assert.Equal("formattedString", result["paramName"]);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        private class CustomFormatter : IFormatterResolver
        {
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                if (typeof(T) == typeof(string))
                {
                    return new StringFormatter<T>();
                }
                return null;
            }

            private class StringFormatter<T> : IMessagePackFormatter<T>
            {
                public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
                {
                    // this method isn't used in our tests
                    readSize = 0;
                    return default;
                }

                public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
                {
                    // string of size 15
                    bytes[offset] = 0xAF;
                    bytes[offset + 1] = (byte)'f';
                    bytes[offset + 2] = (byte)'o';
                    bytes[offset + 3] = (byte)'r';
                    bytes[offset + 4] = (byte)'m';
                    bytes[offset + 5] = (byte)'a';
                    bytes[offset + 6] = (byte)'t';
                    bytes[offset + 7] = (byte)'t';
                    bytes[offset + 8] = (byte)'e';
                    bytes[offset + 9] = (byte)'d';
                    bytes[offset + 10] = (byte)'S';
                    bytes[offset + 11] = (byte)'t';
                    bytes[offset + 12] = (byte)'r';
                    bytes[offset + 13] = (byte)'i';
                    bytes[offset + 14] = (byte)'n';
                    bytes[offset + 15] = (byte)'g';
                    return 16;
                }
            }
        }

        [Fact]
        public async Task CanGetHttpContextFromHubConnectionContext()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var httpContext = new DefaultHttpContext();
                var feature = new TestHttpContextFeature
                {
                    HttpContext = httpContext
                };
                client.Connection.Features.Set<IHttpContextFeature>(feature);
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).OrTimeout()).Result;
                Assert.True((bool)result);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task GetHttpContextFromHubConnectionContextHandlesNull()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).OrTimeout()).Result;
                Assert.False((bool)result);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task AcceptsPingMessages()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.OrTimeout();

                // Send a ping
                await client.SendHubMessageAsync(PingMessage.Instance).OrTimeout();

                // Now do an invocation to make sure we processed the ping message
                var completion = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).OrTimeout();
                Assert.NotNull(completion);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();
            }
        }

        [Fact]
        public async Task DoesNotWritePingMessagesIfSufficientOtherMessagesAreSent()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)));
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                // Echo a bunch of stuff, waiting 10ms between each, until 500ms have elapsed
                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalMilliseconds <= 500.0)
                {
                    await client.SendInvocationAsync("Echo", "foo").OrTimeout();
                    await Task.Delay(10);
                }

                // Shut down
                client.Dispose();

                await connectionHandlerTask.OrTimeout();

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
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)));
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.OrTimeout();

                // Wait 500 ms, but make sure to yield some time up to unblock concurrent threads
                // This is useful on AppVeyor because it's slow enough to end up with no time
                // being available for the endpoint to run.
                for (var i = 0; i < 50; i += 1)
                {
                    client.TickHeartbeat();
                    await Task.Yield();
                    await Task.Delay(10);
                }

                // Shut down
                client.Dispose();

                await connectionHandlerTask.OrTimeout();

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
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.OrTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.OrTimeout();

                client.Connection.Transport.Output.Complete();

                var message = await client.ReadAsync().OrTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(message);
                Assert.Null(closeMessage.Error);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ErrorInHubOnConnectSendsCloseMessageWithError(bool detailedErrors)
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = detailedErrors;
                });
            });
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var message = await client.ReadAsync().OrTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(message);
                if (detailedErrors)
                {
                    Assert.Equal("Connection closed with an error. InvalidOperationException: Hub OnConnected failed.", closeMessage.Error);
                }
                else
                {
                    Assert.Equal("Connection closed with an error.", closeMessage.Error);
                }

                await connectionHandlerTask.OrTimeout();
            }
        }

        public static IEnumerable<object[]> HubTypes()
        {
            yield return new[] { typeof(DynamicTestHub) };
            yield return new[] { typeof(MethodHub) };
            yield return new[] { typeof(HubT) };
        }

        public class TestHttpContextFeature : IHttpContextFeature
        {
            public HttpContext HttpContext { get; set; }
        }
    }
}

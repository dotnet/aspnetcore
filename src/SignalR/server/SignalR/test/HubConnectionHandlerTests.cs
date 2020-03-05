// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubConnectionHandlerTests : VerifiableLoggedTest
    {
        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task HubsAreDisposed()
        {
            using (StartVerifiableLog())
            {
                var trackDispose = new TrackDispose();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(trackDispose), LoggerFactory);
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
        }

        [Fact]
        public async Task AsyncDisposablesInHubsAreSupported()
        {
            using (StartVerifiableLog())
            {
                var trackDispose = new TrackDispose();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s =>
                {
                    s.AddScoped<AsyncDisposable>();
                    s.AddSingleton(trackDispose);
                },
                LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<HubWithAsyncDisposable>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = (await client.InvokeAsync(nameof(HubWithAsyncDisposable.Test)).OrTimeout());
                    Assert.NotNull(result);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask;

                    Assert.Equal(3, trackDispose.DisposeCount);
                }
            }
        }

        [Fact]
        public async Task ConnectionAbortedTokenTriggers()
        {
            using (StartVerifiableLog())
            {
                var state = new ConnectionLifetimeState();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
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
        }

        [Fact]
        public async Task OnDisconnectedAsyncTriggersWhenAbortedTokenCallbackThrows()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ErrorInAbortedTokenHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    var firedOnConnected = (bool)client.Connection.Items[nameof(ErrorInAbortedTokenHub.OnConnectedAsync)];
                    var firedOnDisconnected = (bool)client.Connection.Items[nameof(ErrorInAbortedTokenHub.OnDisconnectedAsync)];

                    Assert.True(firedOnConnected);
                    Assert.True(firedOnDisconnected);
                }
            }
        }

        [Fact]
        public async Task AbortFromHubMethodForcesClientDisconnect()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AbortHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.SendInvocationAsync(nameof(AbortHub.Kill)).OrTimeout();

                    await connectionHandlerTask.OrTimeout();

                    Assert.Null(client.TryRead());
                }
            }
        }

        [Fact]
        public async Task MissingHandshakeAndMessageSentFromHubConnectionCanBeDisposedCleanly()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
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
        }

        [Fact]
        public async Task HandshakeTimesOut()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.Configure<HubOptions>(options =>
                    {
                        options.HandshakeTimeout = TimeSpan.FromMilliseconds(5);
                    });
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<SimpleHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler, false);

                    Assert.NotNull(client.HandshakeResponseMessage);
                    Assert.Equal("Handshake was canceled.", client.HandshakeResponseMessage.Error);

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CanLoadHubContext()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var context = serviceProvider.GetRequiredService<IHubContext<SimpleHub>>();
                await context.Clients.All.SendAsync("Send", "test");
            }
        }

        [Fact]
        public async Task CanLoadTypedHubContext()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var context = serviceProvider.GetRequiredService<IHubContext<SimpleTypedHub, ITypedHubClient>>();
                await context.Clients.All.Send("test");
            }
        }

        [Fact]
        public void FailsToLoadInvalidTypedHubClient()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    serviceProvider.GetRequiredService<IHubContext<SimpleVoidReturningTypedHub, IVoidReturningTypedHubClient>>());
                Assert.Equal($"Cannot generate proxy implementation for '{typeof(IVoidReturningTypedHubClient).FullName}.{nameof(IVoidReturningTypedHubClient.Send)}'. All client proxy methods must return '{typeof(Task).FullName}'.", ex.Message);
            }
        }

        [Fact]
        public async Task HandshakeFailureFromUnknownProtocolSendsResponseWithError()
        {
            using (StartVerifiableLog())
            {
                var hubProtocolMock = new Mock<IHubProtocol>();
                hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

                using (var client = new TestClient(protocol: hubProtocolMock.Object))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    Assert.NotNull(client.HandshakeResponseMessage);
                    Assert.Equal("The protocol 'CustomProtocol' is not supported.", client.HandshakeResponseMessage.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HandshakeFailureFromUnsupportedFormatSendsResponseWithError()
        {
            using (StartVerifiableLog())
            {
                var hubProtocolMock = new Mock<IHubProtocol>();
                hubProtocolMock.Setup(m => m.Name).Returns("CustomProtocol");

                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

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
        }

        [Fact]
        public async Task ConnectionClosedWhenHandshakeLargerThanMaxMessageSize()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory,
                    builder =>
                    {
                        builder.AddSignalR(o =>
                        {
                            o.MaximumReceiveMessageSize = 1;
                        });
                    });

                using (var client = new TestClient())
                {
                    client.SupportedFormats = TransferFormat.Text;

                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler,
                                                            sendHandshakeRequestMessage: true,
                                                            expectedHandshakeResponseMessage: false);

                    var message = await client.ReadAsync(isHandshake: true).OrTimeout();

                    Assert.Equal("Handshake was canceled.", ((HandshakeResponseMessage)message).Error);

                    // Connection closes
                    await connectionHandlerTask.OrTimeout();

                    client.Dispose();
                }
            }
        }

        [Fact]
        public async Task SendingHandshakeRequestInChunksWorks()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);
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
        }

        [Fact]
        public async Task SendingInvocatonInChunksWorks()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);
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
        }

        [Fact]
        public async Task SendingHandshakeRequestAndInvocationInSamePayloadParsesHandshakeAndInvocation()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);
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
        }

        [Fact]
        public async Task HandshakeSuccessSendsResponseWithoutError()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    Assert.NotNull(client.HandshakeResponseMessage);
                    Assert.Null(client.HandshakeResponseMessage.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubMessageOverTheMaxMessageSizeThrows()
        {
            var payload = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"1\", \"target\": \"Echo\", \"arguments\":[\"hello\"]}\u001e");
            var maximumMessageSize = payload.Length - 10;

            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), LoggerFactory,
                    services => services.AddSignalR().AddHubOptions<HubT>(o => o.MaximumReceiveMessageSize = maximumMessageSize));

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connection.Application.Output.WriteAsync(payload);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }

            var exceptionLog = TestSink.Writes.Where(w => string.Equals(w.LoggerName, "Microsoft.AspNetCore.SignalR.HubConnectionHandler") &&
                (w.Exception is InvalidDataException ide));
            Assert.Single(exceptionLog);
            Assert.Equal(exceptionLog.First().Exception.Message, $"The maximum message size of {maximumMessageSize}B was exceeded. The message size can be configured in AddHubOptions.");
        }

        [Fact]
        public async Task ChunkedHubMessageOverTheMaxMessageSizeThrows()
        {
            var payload = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"1\", \"target\": \"Echo\", \"arguments\":[\"hello\"]}\u001e");
            var maximumMessageSize = payload.Length - 10;

            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), LoggerFactory,
                    services => services.AddSignalR().AddHubOptions<HubT>(o => o.MaximumReceiveMessageSize = maximumMessageSize));

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connection.Application.Output.WriteAsync(payload.AsMemory(0, payload.Length / 2));
                    await client.Connection.Application.Output.WriteAsync(payload.AsMemory(payload.Length / 2));

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }

            var exceptionLog = TestSink.Writes.Where(w => string.Equals(w.LoggerName, "Microsoft.AspNetCore.SignalR.HubConnectionHandler") &&
                (w.Exception is InvalidDataException ide));
            Assert.Single(exceptionLog);
            Assert.Equal(exceptionLog.First().Exception.Message, $"The maximum message size of {maximumMessageSize}B was exceeded. The message size can be configured in AddHubOptions.");
        }

        [Fact]
        public async Task ManyHubMessagesOneOverTheMaxMessageSizeThrows()
        {
            var payload1 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"1\", \"target\": \"Echo\", \"arguments\":[\"one\"]}\u001e");
            var payload2 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"2\", \"target\": \"Echo\", \"arguments\":[\"two\"]}\u001e");
            var payload3 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"3\", \"target\": \"Echo\", \"arguments\":[\"three\"]}\u001e");

            // Between the first and the second payload so we'll end up slicing with some remaining in the slice for
            // the next message
            var maximumMessageSize = payload1.Length + 1;
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), LoggerFactory,
                    services => services.AddSignalR().AddHubOptions<HubT>(o => o.MaximumReceiveMessageSize = maximumMessageSize));

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    client.Connection.Application.Output.Write(payload1);
                    client.Connection.Application.Output.Write(payload2);
                    client.Connection.Application.Output.Write(payload3);
                    await client.Connection.Application.Output.FlushAsync();

                    // 2 invocations should be processed
                    var completionMessage = await client.ReadAsync().OrTimeout() as CompletionMessage;
                    Assert.NotNull(completionMessage);
                    Assert.Equal("1", completionMessage.InvocationId);
                    Assert.Equal("one", completionMessage.Result);

                    completionMessage = await client.ReadAsync().OrTimeout() as CompletionMessage;
                    Assert.NotNull(completionMessage);
                    Assert.Equal("2", completionMessage.InvocationId);
                    Assert.Equal("two", completionMessage.Result);

                    // We never receive the 3rd message since it was over the maximum message size
                    CloseMessage closeMessage = await client.ReadAsync().OrTimeout() as CloseMessage;
                    Assert.NotNull(closeMessage);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }

            var exceptionLog = TestSink.Writes.Where(w => string.Equals(w.LoggerName, "Microsoft.AspNetCore.SignalR.HubConnectionHandler") &&
                (w.Exception is InvalidDataException ide));
            Assert.Single(exceptionLog);
            Assert.Equal(exceptionLog.First().Exception.Message, $"The maximum message size of {maximumMessageSize}B was exceeded. The message size can be configured in AddHubOptions.");
        }

        [Fact]
        public async Task ManyHubMessagesUnderTheMessageSizeButConfiguredWithMax()
        {
            var payload1 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"1\", \"target\": \"Echo\", \"arguments\":[\"one\"]}\u001e");
            var payload2 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"2\", \"target\": \"Echo\", \"arguments\":[\"two\"]}\u001e");
            var payload3 = Encoding.UTF8.GetBytes("{\"type\":1, \"invocationId\":\"3\", \"target\": \"Echo\", \"arguments\":[\"three\"]}\u001e");

            // Bigger than all 3 messages
            var maximumMessageSize = payload3.Length + 10;

            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), LoggerFactory,
                    services => services.AddSignalR().AddHubOptions<HubT>(o => o.MaximumReceiveMessageSize = maximumMessageSize));

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    client.Connection.Application.Output.Write(payload1);
                    client.Connection.Application.Output.Write(payload2);
                    client.Connection.Application.Output.Write(payload3);
                    await client.Connection.Application.Output.FlushAsync();

                    // 2 invocations should be processed
                    var completionMessage = await client.ReadAsync().OrTimeout() as CompletionMessage;
                    Assert.NotNull(completionMessage);
                    Assert.Equal("1", completionMessage.InvocationId);
                    Assert.Equal("one", completionMessage.Result);

                    completionMessage = await client.ReadAsync().OrTimeout() as CompletionMessage;
                    Assert.NotNull(completionMessage);
                    Assert.Equal("2", completionMessage.InvocationId);
                    Assert.Equal("two", completionMessage.Result);

                    completionMessage = await client.ReadAsync().OrTimeout() as CompletionMessage;
                    Assert.NotNull(completionMessage);
                    Assert.Equal("3", completionMessage.InvocationId);
                    Assert.Equal("three", completionMessage.Result);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HandshakeFailureFromIncompatibleProtocolVersionSendsResponseWithError()
        {
            using (StartVerifiableLog())
            {
                var hubProtocolMock = new Mock<IHubProtocol>();
                hubProtocolMock.Setup(m => m.Name).Returns("json");
                hubProtocolMock.Setup(m => m.Version).Returns(9001);

                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

                using (var client = new TestClient(protocol: hubProtocolMock.Object))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    Assert.NotNull(client.HandshakeResponseMessage);
                    Assert.Equal("The server does not support version 9001 of the 'json' protocol.", client.HandshakeResponseMessage.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task ConnectionClosesOnServerWithPartialHandshakeMessageAndCompletedPipe()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

                using (var client = new TestClient())
                {
                    // partial handshake
                    var payload = Encoding.UTF8.GetBytes("{\"protocol\": \"json\",\"ver");
                    await client.Connection.Application.Output.WriteAsync(payload).OrTimeout();

                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler, sendHandshakeRequestMessage: false, expectedHandshakeResponseMessage: false);
                    // Complete the pipe to 'close' the connection
                    client.Connection.Application.Output.Complete();

                    // This will never complete as the pipe was completed and nothing can be written to it
                    var handshakeReadTask = client.ReadAsync(true);

                    // Check that the connection was closed on the server
                    await connectionHandlerTask.OrTimeout();
                    Assert.False(handshakeReadTask.IsCompleted);

                    client.Dispose();
                }
            }
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfLifetimeManagerOnConnectedAsyncThrows()
        {
            using (StartVerifiableLog())
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
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task HubOnDisconnectedAsyncCalledIfHubOnConnectedAsyncThrows()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                       writeContext.EventId.Name == "ErrorDispatchingHubEvent";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var mockLifetimeManager = new Mock<HubLifetimeManager<OnConnectedThrowsHub>>();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSingleton(mockLifetimeManager.Object);
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task LifetimeManagerOnDisconnectedAsyncCalledIfHubOnDisconnectedAsyncThrows()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                       writeContext.EventId.Name == "ErrorDispatchingHubEvent";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var mockLifetimeManager = new Mock<HubLifetimeManager<OnDisconnectedThrowsHub>>();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSingleton(mockLifetimeManager.Object);
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodCanReturnValueFromTask()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodCanReturnValueFromValueTask()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = (await client.InvokeAsync(nameof(MethodHub.ValueTaskValueMethod)).OrTimeout()).Result;

                    // json serializer makes this a long
                    Assert.Equal(43L, result);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubMethodCanReturnValueTask()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = (await client.InvokeAsync(nameof(MethodHub.ValueTaskMethod)).OrTimeout()).Result;

                    Assert.Null(result);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubMethodsAreCaseInsensitive(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [InlineData(nameof(MethodHub.MethodThatThrows), true)]
        [InlineData(nameof(MethodHub.MethodThatYieldsFailedTask), false)]
        public async Task HubMethodCanThrowOrYieldFailedTask(string methodName, bool detailedErrors)
        {
            var hasErrorLog = false;
            bool ExpectedErrors(WriteContext writeContext)
            {
                var expected = writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
                if (expected)
                {
                    hasErrorLog = true;
                    return true;
                }
                return false;
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSignalR(options =>
                    {
                        options.EnableDetailedErrors = detailedErrors;
                    });
                }, LoggerFactory);

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

            Assert.True(hasErrorLog);
        }

        [Fact]
        public async Task DetailedExceptionEvenWhenNotExplicitlySet()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var methodName = nameof(MethodHub.ThrowHubException);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var message = await client.InvokeAsync(methodName).OrTimeout();

                    Assert.Equal($"An unexpected error occurred invoking '{methodName}' on the server. HubException: This is a hub exception", message.Error);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubMethodDoesNotSendResultWhenInvocationIsNonBlocking()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodCanBeVoid()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodCanBeRenamedWithAttribute()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodNameAttributeIsInherited()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Theory]
        [InlineData(nameof(MethodHub.VoidMethod))]
        [InlineData(nameof(MethodHub.MethodThatThrows))]
        [InlineData(nameof(MethodHub.ValueMethod))]
        public async Task NonBlockingInvocationDoesNotSendCompletion(string methodName)
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return methodName == nameof(MethodHub.MethodThatThrows) && writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task HubMethodWithMultiParam()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task CanCallInheritedHubMethodFromInheritingHub()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task CanCallOverridenVirtualHubMethod()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task CannotCallOverriddenBaseHubMethod()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = await client.InvokeAsync(nameof(MethodHub.OnDisconnectedAsync)).OrTimeout();

                    Assert.Equal("Failed to invoke 'OnDisconnectedAsync' due to an error on the server. HubException: Method does not exist.", result.Error);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public void HubsCannotHaveOverloadedMethods()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task CannotCallStaticHubMethods()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = await client.InvokeAsync(nameof(MethodHub.StaticMethod)).OrTimeout();

                    Assert.Equal("Failed to invoke 'StaticMethod' due to an error on the server. HubException: Method does not exist.", result.Error);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CannotCallObjectMethodsOnHub()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = await client.InvokeAsync(nameof(MethodHub.ToString)).OrTimeout();
                    Assert.Equal("Failed to invoke 'ToString' due to an error on the server. HubException: Method does not exist.", result.Error);

                    result = await client.InvokeAsync(nameof(MethodHub.GetHashCode)).OrTimeout();
                    Assert.Equal("Failed to invoke 'GetHashCode' due to an error on the server. HubException: Method does not exist.", result.Error);

                    result = await client.InvokeAsync(nameof(MethodHub.Equals)).OrTimeout();
                    Assert.Equal("Failed to invoke 'Equals' due to an error on the server. HubException: Method does not exist.", result.Error);

                    result = await client.InvokeAsync(nameof(MethodHub.ReferenceEquals)).OrTimeout();
                    Assert.Equal("Failed to invoke 'ReferenceEquals' due to an error on the server. HubException: Method does not exist.", result.Error);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CannotCallDisposeMethodOnHub()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var result = await client.InvokeAsync(nameof(MethodHub.Dispose)).OrTimeout();

                    Assert.Equal("Failed to invoke 'Dispose' due to an error on the server. HubException: Method does not exist.", result.Error);

                    // kill the connection
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public void CannotHaveGenericMethodOnHub()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

                var exception = Assert.Throws<NotSupportedException>(() => serviceProvider.GetService<HubConnectionHandler<GenericMethodHub>>());

                Assert.Equal("Method 'GenericMethod' is a generic method which is not supported on a Hub.", exception.Message);
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task BroadcastHubMethodSendsToAllClients(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Fact]
        public async Task SendArraySendsArrayToAllClients()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthers(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToCaller(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Fact]
        public async Task FailsToInitializeInvalidTypedHub()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                       writeContext.EventId.Name == "ErrorDispatchingHubEvent";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(SimpleVoidReturningTypedHub), loggerFactory: LoggerFactory);

                using (var firstClient = new TestClient())
                {
                    // ConnectAsync returns a Task<Task> and it's the INNER Task that will be faulted.
                    var connectionTask = await firstClient.ConnectAsync(connectionHandler);

                    // We should get a close frame now
                    var close = Assert.IsType<CloseMessage>(await firstClient.ReadAsync());
                    Assert.Equal("Connection closed with an error.", close.Error);
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToAllExcept(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleClients(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToMultipleUsers(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

                using (var firstClient = new TestClient(userIdentifier: "userA"))
                using (var secondClient = new TestClient(userIdentifier: "userB"))
                using (var thirdClient = new TestClient(userIdentifier: "userC"))
                {
                    var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                    var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);
                    var thirdConnectionHandlerTask = await thirdClient.ConnectAsync(connectionHandler);

                    await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).OrTimeout();

                    await firstClient.SendInvocationAsync(nameof(MethodHub.SendToMultipleUsers), new[] { "userB", "userC" }, "Second and Third").OrTimeout();

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanAddAndSendToGroup(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToGroupExcept(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task SendToOthersInGroup(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task InvokeMultipleGroups(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Fact]
        public async Task RemoveFromGroupWhenNotInGroupDoesNotFail()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToUser(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

                using (var firstClient = new TestClient(userIdentifier: "userA"))
                using (var secondClient = new TestClient(userIdentifier: "userB"))
                {
                    var firstConnectionHandlerTask = await firstClient.ConnectAsync(connectionHandler);
                    var secondConnectionHandlerTask = await secondClient.ConnectAsync(connectionHandler);

                    await Task.WhenAll(firstClient.Connected, secondClient.Connected).OrTimeout();

                    await firstClient.SendInvocationAsync("ClientSendMethod", "userB", "test").OrTimeout();

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
        }

        [Theory]
        [MemberData(nameof(HubTypes))]
        public async Task HubsCanSendToConnection(Type hubType)
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(hubType, loggerFactory: LoggerFactory);

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
        }

        [Fact]
        public async Task DelayedSendTest()
        {
            using (StartVerifiableLog())
            {
                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT), loggerFactory: LoggerFactory);

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
        }

        [Theory]
        [MemberData(nameof(StreamingMethodAndHubProtocols))]
        public async Task HubsCanStreamResponses(string method, string protocolName)
        {
            using (StartVerifiableLog())
            {
                var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
                var invocationBinder = new Mock<IInvocationBinder>();
                invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(string));

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
        }

        [Fact]
        public async Task NonErrorCompletionSentWhenStreamCanceledFromClient()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();

                    var invocationId = Guid.NewGuid().ToString("N");
                    await client.SendHubMessageAsync(new StreamInvocationMessage(invocationId, nameof(StreamingHub.BlockingStream), Array.Empty<object>()));

                    // cancel the Streaming method
                    await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).OrTimeout();

                    var hubMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().OrTimeout());
                    Assert.Equal(invocationId, hubMessage.InvocationId);
                    Assert.Null(hubMessage.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReceiveCorrectErrorFromStreamThrowing(bool detailedErrors)
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                builder.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = detailedErrors;
                }), LoggerFactory);
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
        }

        [Fact]
        public async Task CanSendToConnectionsWithDifferentProtocols()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client1 = new TestClient(protocol: new NewtonsoftJsonHubProtocol()))
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
        }

        public static IEnumerable<object[]> StreamingMethodAndHubProtocols
        {
            get
            {
                var methods = new[]
                {
                    nameof(StreamingHub.CounterChannel),
                    nameof(StreamingHub.CounterChannelAsync),
                    nameof(StreamingHub.CounterChannelValueTaskAsync),
                    nameof(StreamingHub.CounterAsyncEnumerable),
                    nameof(StreamingHub.CounterAsyncEnumerableAsync),
                    nameof(StreamingHub.CounterAsyncEnumerableImpl),
                    nameof(StreamingHub.AsyncEnumerableIsPreferredOverChannelReader),
                };

                foreach (var method in methods)
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
            using (StartVerifiableLog())
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
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task AuthorizedConnectionCanInvokeHubMethodWithAuthorization()
        {
            using (StartVerifiableLog())
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
                }, LoggerFactory);

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
        }

        private class TestAuthHandler : IAuthorizationHandler
        {
            public Task HandleAsync(AuthorizationHandlerContext context)
            {
                Assert.NotNull(context.Resource);
                var resource = Assert.IsType<HubInvocationContext>(context.Resource);
                Assert.Equal(typeof(MethodHub), resource.HubType);
                Assert.Equal(nameof(MethodHub.MultiParamAuthMethod), resource.HubMethodName);
                Assert.Equal(2, resource.HubMethodArguments?.Count);
                Assert.Equal("Hello", resource.HubMethodArguments[0]);
                Assert.Equal("World!", resource.HubMethodArguments[1]);
                Assert.NotNull(resource.Context);
                Assert.Equal(context.User, resource.Context.User);
                Assert.NotNull(resource.Context.GetHttpContext());

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task HubMethodWithAuthorizationProvidesResourceToAuthHandlers()
        {
            using (StartVerifiableLog())
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

                    services.AddSingleton<IAuthorizationHandler, TestAuthHandler>();
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    client.Connection.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "name") }));

                    // Setup a HttpContext to make sure it flows to the AuthHandler correctly
                    var httpConnectionContext = new HttpContextFeatureImpl();
                    httpConnectionContext.HttpContext = new DefaultHttpContext();
                    client.Connection.Features.Set<IHttpContextFeature>(httpConnectionContext);

                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();

                    var message = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "Hello", "World!").OrTimeout();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubOptionsCanUseCustomJsonSerializerSettings()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services
                        .AddSignalR()
                        .AddNewtonsoftJsonProtocol(o =>
                        {
                            o.PayloadSerializerSettings = new JsonSerializerSettings
                            {
                                ContractResolver = new DefaultContractResolver()
                            };
                        });
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task JsonHubProtocolUsesCamelCasingByDefault()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
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
        }

        [Fact]
        public async Task HubOptionsCanUseCustomMessagePackSettings()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR()
                        .AddMessagePackProtocol(options =>
                        {
                            options.FormatterResolvers.Insert(0, new CustomFormatter());
                        });
                }, LoggerFactory);

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
        }

        [Fact]
        public async Task HubOptionsCanNotHaveNullSupportedProtocols()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(o =>
                    {
                        o.SupportedProtocols = null;
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                var msgPackOptions = serviceProvider.GetRequiredService<IOptions<MessagePackHubProtocolOptions>>();
                using (var client = new TestClient(protocol: new MessagePackHubProtocol(msgPackOptions)))
                {
                    client.SupportedFormats = TransferFormat.Binary;
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await await client.ConnectAsync(connectionHandler, expectedHandshakeResponseMessage: false)).OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubOptionsCanNotHaveEmptySupportedProtocols()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(o =>
                    {
                        o.SupportedProtocols = new List<string>();
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                var msgPackOptions = serviceProvider.GetRequiredService<IOptions<MessagePackHubProtocolOptions>>();
                using (var client = new TestClient(protocol: new MessagePackHubProtocol(msgPackOptions)))
                {
                    client.SupportedFormats = TransferFormat.Binary;
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await await client.ConnectAsync(connectionHandler, expectedHandshakeResponseMessage: false)).OrTimeout();
                }
            }
        }

        [Fact]
        public async Task ConnectionUserIdIsAssignedByUserIdProvider()
        {
            using (StartVerifiableLog())
            {
                var firstRequest = true;
                var userIdProvider = new TestUserIdProvider(c =>
                {
                    if (firstRequest)
                    {
                        firstRequest = false;
                        return "client1";
                    }
                    else
                    {
                        return "client2";
                    }
                });
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IUserIdProvider>(userIdProvider);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client1 = new TestClient())
                using (var client2 = new TestClient())
                {
                    var connectionHandlerTask1 = await client1.ConnectAsync(connectionHandler);
                    var connectionHandlerTask2 = await client2.ConnectAsync(connectionHandler);

                    await client1.Connected.OrTimeout();
                    await client2.Connected.OrTimeout();

                    await client2.SendInvocationAsync(nameof(MethodHub.SendToMultipleUsers), new[] { "client1" }, "Hi!").OrTimeout();

                    var message = (InvocationMessage)await client1.ReadAsync().OrTimeout();

                    Assert.Equal("Send", message.Target);
                    Assert.Collection(message.Arguments, arg => Assert.Equal("Hi!", arg));

                    client1.Dispose();
                    client2.Dispose();

                    await connectionHandlerTask1.OrTimeout();
                    await connectionHandlerTask2.OrTimeout();

                    // Read the completion, then we should have nothing left in client2's queue
                    Assert.IsType<CompletionMessage>(client2.TryRead());
                    Assert.IsType<CloseMessage>(client2.TryRead());
                    Assert.Null(client2.TryRead());
                }
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
                public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    // this method isn't used in our tests
                    return default;
                }

                public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
                {
                    writer.Write("formattedString");
                }
            }
        }

        [Fact]
        public async Task CanGetHttpContextFromHubConnectionContext()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task GetHttpContextFromHubConnectionContextHandlesNull()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);

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
        }

        [Fact]
        public async Task AcceptsPingMessages()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
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
        }

        [Fact]
        public async Task DoesNotWritePingMessagesIfSufficientOtherMessagesAreSent()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                    services.Configure<HubOptions>(options =>
                        options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)), LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
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
        }

        [Fact]
        public async Task WritesPingMessageIfNothingWrittenWhenKeepAliveIntervalElapses()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                    services.Configure<HubOptions>(options =>
                        options.KeepAliveInterval = TimeSpan.FromMilliseconds(100)), LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
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
        }

        [Fact]
        public async Task ConnectionNotTimedOutIfClientNeverPings()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                    services.Configure<HubOptions>(options =>
                        options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(100)), LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                    await client.Connected.OrTimeout();
                    // This is a fake client -- it doesn't auto-ping to signal

                    // We go over the 100 ms timeout interval...
                    await Task.Delay(120);
                    client.TickHeartbeat();

                    // but client should still be open, since it never pinged to activate the timeout checking
                    Assert.False(connectionHandlerTask.IsCompleted);
                }
            }
        }

        [Fact]
        public async Task ConnectionTimesOutIfInitialPingAndThenNoMessages()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                    services.Configure<HubOptions>(options =>
                        options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(100)), LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                    await client.Connected.OrTimeout();
                    await client.SendHubMessageAsync(PingMessage.Instance);

                    await Task.Delay(300);
                    client.TickHeartbeat();

                    await Task.Delay(300);
                    client.TickHeartbeat();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        [QuarantinedTest]
        public async Task ReceivingMessagesPreventsConnectionTimeoutFromOccuring()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                    services.Configure<HubOptions>(options =>
                         options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(300)), LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                    await client.Connected.OrTimeout();
                    await client.SendHubMessageAsync(PingMessage.Instance);

                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(100);
                        client.TickHeartbeat();
                        await client.SendHubMessageAsync(PingMessage.Instance);
                    }

                    Assert.False(connectionHandlerTask.IsCompleted);
                }
            }
        }

        internal class PipeReaderWrapper : PipeReader
        {
            private readonly PipeReader _originalPipeReader;
            private TaskCompletionSource<object> _waitForRead;
            private object _lock = new object();

            public PipeReaderWrapper(PipeReader pipeReader)
            {
                _originalPipeReader = pipeReader;
                _waitForRead = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public override void AdvanceTo(SequencePosition consumed) =>
                _originalPipeReader.AdvanceTo(consumed);

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) =>
                _originalPipeReader.AdvanceTo(consumed, examined);

            public override void CancelPendingRead() =>
                _originalPipeReader.CancelPendingRead();

            public override void Complete(Exception exception = null) =>
                _originalPipeReader.Complete(exception);

            public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                lock (_lock)
                {
                    _waitForRead.SetResult(null);
                }

                try
                {
                    return await _originalPipeReader.ReadAsync(cancellationToken);
                }
                finally
                {
                    lock (_lock)
                    {
                        _waitForRead = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                }
            }

            public override bool TryRead(out ReadResult result) =>
                _originalPipeReader.TryRead(out result);

            public Task WaitForReadStart()
            {
                lock (_lock)
                {
                    return _waitForRead.Task;
                }
            }
        }

        internal class CustomDuplex : IDuplexPipe
        {
            private readonly IDuplexPipe _originalDuplexPipe;
            public readonly PipeReaderWrapper WrappedPipeReader;

            public CustomDuplex(IDuplexPipe duplexPipe)
            {
                _originalDuplexPipe = duplexPipe;
                WrappedPipeReader = new PipeReaderWrapper(_originalDuplexPipe.Input);
            }

            public PipeReader Input => WrappedPipeReader;

            public PipeWriter Output => _originalDuplexPipe.Output;
        }

        [Fact]
        public async Task HubMethodInvokeDoesNotCountTowardsClientTimeout()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.Configure<HubOptions>(options =>
                         options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(0));
                    services.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                using (var client = new TestClient(new JsonHubProtocol()))
                {
                    var customDuplex = new CustomDuplex(client.Connection.Transport);
                    client.Connection.Transport = customDuplex;

                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                    // This starts the timeout logic
                    await client.SendHubMessageAsync(PingMessage.Instance);

                    // Call long running hub method
                    var hubMethodTask = client.InvokeAsync(nameof(LongRunningHub.LongRunningMethod));
                    await tcsService.StartedMethod.Task.OrTimeout();

                    // Tick heartbeat while hub method is running to show that close isn't triggered
                    client.TickHeartbeat();

                    // Unblock long running hub method
                    tcsService.EndMethod.SetResult(null);

                    await hubMethodTask.OrTimeout();

                    // There is a small window when the hub method finishes and the timer starts again
                    // So we need to delay a little before ticking the heart beat.
                    // We do this by waiting until we know the HubConnectionHandler code is in pipe.ReadAsync()
                    await customDuplex.WrappedPipeReader.WaitForReadStart().OrTimeout();

                    // Tick heartbeat again now that we're outside of the hub method
                    client.TickHeartbeat();

                    // Connection is closed
                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task EndingConnectionSendsCloseMessageWithNoError()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
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
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ErrorInHubOnConnectSendsCloseMessageWithError(bool detailedErrors)
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                       writeContext.EventId.Name == "ErrorDispatchingHubEvent";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSignalR(options =>
                    {
                        options.EnableDetailedErrors = detailedErrors;
                    });
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
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
        }

        [Fact]
        public async Task StreamingInvocationsDoNotBlockOtherInvocations()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

                using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    // Blocking streaming invocation to test that other invocations can still run
                    await client.SendHubMessageAsync(new StreamInvocationMessage("1", nameof(StreamingHub.BlockingStream), Array.Empty<object>())).OrTimeout();

                    var completion = await client.InvokeAsync(nameof(StreamingHub.NonStream)).OrTimeout();
                    Assert.Equal(42L, completion.Result);

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task InvocationsRunInOrder()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                // Because we use PipeScheduler.Inline the hub invocations will run inline until they wait, which happens inside the LongRunningMethod call
                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    // Long running hub invocation to test that other invocations will not run until it is completed
                    await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod), nonBlocking: false).OrTimeout();
                    // Wait for the long running method to start
                    await tcsService.StartedMethod.Task.OrTimeout();

                    // Invoke another hub method which will wait for the first method to finish
                    await client.SendInvocationAsync(nameof(LongRunningHub.SimpleMethod), nonBlocking: false).OrTimeout();
                    // Both invocations should be waiting now
                    Assert.Null(client.TryRead());

                    // Release the long running hub method
                    tcsService.EndMethod.TrySetResult(null);

                    // Long running hub method result
                    var firstResult = await client.ReadAsync().OrTimeout();

                    var longRunningCompletion = Assert.IsType<CompletionMessage>(firstResult);
                    Assert.Equal(12L, longRunningCompletion.Result);

                    // simple hub method result
                    var secondResult = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(secondResult);
                    Assert.Equal(21L, simpleCompletion.Result);

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task StreamInvocationsBlockOtherInvocationsUntilTheyStartStreaming()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                    builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                // Because we use PipeScheduler.Inline the hub invocations will run inline until they wait, which happens inside the LongRunningMethod call
                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    // Long running hub invocation to test that other invocations will not run until it is completed
                    var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.LongRunningStream), null).OrTimeout();
                    // Wait for the long running method to start
                    await tcsService.StartedMethod.Task.OrTimeout();

                    // Invoke another hub method which will wait for the first method to finish
                    await client.SendInvocationAsync(nameof(LongRunningHub.SimpleMethod), nonBlocking: false).OrTimeout();
                    // Both invocations should be waiting now
                    Assert.Null(client.TryRead());

                    // Release the long running hub method
                    tcsService.EndMethod.TrySetResult(null);

                    // simple hub method result
                    var result = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                    Assert.Equal(21L, simpleCompletion.Result);

                    var hubActivator = serviceProvider.GetService<IHubActivator<LongRunningHub>>() as CustomHubActivator<LongRunningHub>;

                    await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).OrTimeout();

                    // Completion message for canceled Stream
                    await client.ReadAsync().OrTimeout();

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    // OnConnectedAsync, SimpleMethod, LongRunningStream, OnDisconnectedAsync
                    Assert.Equal(4, hubActivator.ReleaseCount);
                }
            }
        }

        [Fact]
        public async Task ServerSendsCloseWithErrorWhenConnectionClosedWithPartialMessage()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options => options.EnableDetailedErrors = true);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<SimpleHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    await client.Connection.Application.Output.WriteAsync(Encoding.UTF8.GetBytes(new[] { '{' })).OrTimeout();

                    // Close connection
                    client.Connection.Application.Output.Complete();

                    // Ignore message from OnConnectedAsync
                    await client.ReadAsync().OrTimeout();

                    var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().OrTimeout());

                    Assert.Equal("Connection closed with an error. InvalidDataException: Connection terminated while reading a message.", closeMessage.Error);

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task StreamUploadBufferCapacityBlocksOtherInvocations()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.Configure<HubOptions>(options =>
                {
                    options.StreamBufferCapacity = 1;
                });
            });

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                await client.BeginUploadStreamAsync("invocationId", nameof(MethodHub.StreamDontRead), new[] { "id" }, Array.Empty<object>()).OrTimeout();

                foreach (var letter in new[] { "A", "B", "C", "D", "E" })
                {
                    await client.SendHubMessageAsync(new StreamItemMessage("id", letter)).OrTimeout();
                }

                var ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
                {
                    await client.SendInvocationAsync("Echo", "test");
                    var result = (CompletionMessage)await client.ReadAsync().OrTimeout(5000);
                });
            }
        }

        [Fact]
        public async Task UploadStringsToConcat()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), new[] { "id" }, Array.Empty<object>());

                foreach (var letter in new[] { "B", "E", "A", "N", "E", "D" })
                {
                    await client.SendHubMessageAsync(new StreamItemMessage("id", letter)).OrTimeout();
                }

                await client.SendHubMessageAsync(CompletionMessage.Empty("id")).OrTimeout();
                var result = (CompletionMessage)await client.ReadAsync().OrTimeout();

                Assert.Equal("BEANED", result.Result);
            }
        }

        [Fact]
        public async Task UploadStreamedObjects()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadArray), new[] { "id" }, Array.Empty<object>());

                var objects = new[] { new SampleObject("solo", 322), new SampleObject("ggez", 3145) };
                foreach (var thing in objects)
                {
                    await client.SendHubMessageAsync(new StreamItemMessage("id", thing)).OrTimeout();
                }

                await client.SendHubMessageAsync(CompletionMessage.Empty("id")).OrTimeout();
                var response = (CompletionMessage)await client.ReadAsync().OrTimeout();
                var result = ((JArray)response.Result).ToArray<object>();

                Assert.Equal(objects[0].Foo, ((JContainer)result[0])["foo"]);
                Assert.Equal(objects[0].Bar, ((JContainer)result[0])["bar"]);
                Assert.Equal(objects[1].Foo, ((JContainer)result[1])["foo"]);
                Assert.Equal(objects[1].Bar, ((JContainer)result[1])["bar"]);
            }
        }

        [Fact]
        public async Task UploadManyStreams()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                var ids = new[] { "0", "1", "2" };

                foreach (string id in ids)
                {
                    await client.BeginUploadStreamAsync("invocation_" + id, nameof(MethodHub.StreamingConcat), new[] { id }, Array.Empty<object>());
                }

                var words = new[] { "zygapophyses", "qwerty", "abcd" };
                var pos = new[] { 0, 0, 0 };
                var order = new[] { 2, 2, 0, 2, 1, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 1, 0, 0, 0, 1, 1, 1 };

                foreach (var spot in order)
                {
                    await client.SendHubMessageAsync(new StreamItemMessage(spot.ToString(), words[spot][pos[spot]])).OrTimeout();
                    pos[spot] += 1;
                }

                foreach (string id in new[] { "0", "2", "1" })
                {
                    await client.SendHubMessageAsync(CompletionMessage.Empty(id)).OrTimeout();
                    var response = await client.ReadAsync().OrTimeout();
                    Debug.Write(response);
                    Assert.Equal(words[int.Parse(id)], ((CompletionMessage)response).Result);
                }
            }
        }

        [Fact]
        public async Task ConnectionAbortedIfSendFailsWithProtocolError()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options => options.EnableDetailedErrors = true);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    await client.SendInvocationAsync(nameof(MethodHub.ProtocolError)).OrTimeout();
                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact(Skip = "Magic auto cast not supported")]
        public async Task UploadStreamItemInvalidTypeAutoCasts()
        {
            using (StartVerifiableLog())
            {
                // NOTE -- json.net is flexible here, and casts for us

                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, Array.Empty<object>()).OrTimeout();

                    // send integers that are then cast to strings
                    await client.SendHubMessageAsync(new StreamItemMessage("id", 5)).OrTimeout();
                    await client.SendHubMessageAsync(new StreamItemMessage("id", 10)).OrTimeout();

                    await client.SendHubMessageAsync(CompletionMessage.Empty("id")).OrTimeout();
                    var response = (CompletionMessage)await client.ReadAsync().OrTimeout();

                    Assert.Null(response.Error);
                    Assert.Equal("510", response.Result);
                }
            }
        }

        [Fact]
        public async Task ServerReportsProtocolMinorVersion()
        {
            using (StartVerifiableLog())
            {
                var testProtocol = new Mock<IHubProtocol>();
                testProtocol.Setup(m => m.Name).Returns("CustomProtocol");
                testProtocol.Setup(m => m.IsVersionSupported(It.IsAny<int>())).Returns(true);
                testProtocol.Setup(m => m.TransferFormat).Returns(TransferFormat.Binary);

                var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(HubT),
                    LoggerFactory, (services) => services.AddSingleton<IHubProtocol>(testProtocol.Object));

                using (var client = new TestClient(protocol: testProtocol.Object))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    Assert.NotNull(client.HandshakeResponseMessage);

                    client.Dispose();
                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task UploadStreamItemInvalidType()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.BeginUploadStreamAsync("invocationId", nameof(MethodHub.TestTypeCastingErrors), new[] { "channelId" }, Array.Empty<object>()).OrTimeout();

                    // client is running wild, sending strings not ints.
                    // this error should be propogated to the user's HubMethod code
                    await client.SendHubMessageAsync(new StreamItemMessage("channelId", "not a number")).OrTimeout();
                    var response = await client.ReadAsync().OrTimeout();

                    Assert.Equal(typeof(CompletionMessage), response.GetType());
                    Assert.Equal("error identified and caught", (string)((CompletionMessage)response).Result);
                }
            }
        }

        [Fact]
        public async Task UploadStreamItemInvalidId()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options => options.EnableDetailedErrors = true);
                }, loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.SendHubMessageAsync(new StreamItemMessage("fake_id", "not a number")).OrTimeout();

                    var message = client.TryRead();
                    Assert.Null(message);
                }
            }

            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                w.EventId.Name == "ClosingStreamWithBindingError"));
        }

        [Fact]
        public async Task UploadStreamCompleteInvalidId()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options => options.EnableDetailedErrors = true);
                }, loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.SendHubMessageAsync(CompletionMessage.Empty("fake_id")).OrTimeout();

                    var message = client.TryRead();
                    Assert.Null(message);
                }
            }

            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                w.EventId.Name == "UnexpectedStreamCompletion"));
        }

        public static string CustomErrorMessage = "custom error for testing ::::)";

        [Fact]
        public async Task UploadStreamCompleteWithError()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.TestCustomErrorPassing), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();
                    await client.SendHubMessageAsync(CompletionMessage.WithError("id", CustomErrorMessage)).OrTimeout();

                    var response = (CompletionMessage)await client.ReadAsync().OrTimeout();
                    Assert.True((bool)response.Result);
                }
            }
        }

        [Fact]
        public async Task UploadStreamWithTooManyStreamsFails()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id", "id2" }, args: Array.Empty<object>()).OrTimeout();

                    var response = (CompletionMessage)await client.ReadAsync().OrTimeout();
                    Assert.Equal("An unexpected error occurred invoking 'StreamingConcat' on the server. HubException: Client sent 2 stream(s), Hub method expects 1.", response.Error);
                }
            }
        }

        [Fact]
        public async Task UploadStreamWithTooFewStreamsFails()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    await client.ConnectAsync(connectionHandler).OrTimeout();
                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: Array.Empty<string>(), args: Array.Empty<object>()).OrTimeout();

                    var response = (CompletionMessage)await client.ReadAsync().OrTimeout();
                    Assert.Equal("An unexpected error occurred invoking 'StreamingConcat' on the server. HubException: Client sent 0 stream(s), Hub method expects 1.", response.Error);
                }
            }
        }

        [Fact]
        public async Task UploadStreamReleasesHubActivatorOnceComplete()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();

                    await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).OrTimeout();
                    await client.SendHubMessageAsync(new StreamItemMessage("id", " world")).OrTimeout();
                    await client.SendHubMessageAsync(CompletionMessage.Empty("id")).OrTimeout();
                    var result = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                    Assert.Equal("hello world", simpleCompletion.Result);

                    var hubActivator = serviceProvider.GetService<IHubActivator<MethodHub>>() as CustomHubActivator<MethodHub>;

                    // OnConnectedAsync and StreamingConcat hubs have been disposed
                    Assert.Equal(2, hubActivator.ReleaseCount);

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task UploadStreamFromSendReleasesHubActivatorOnceComplete()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var hubActivator = serviceProvider.GetService<IHubActivator<MethodHub>>() as CustomHubActivator<MethodHub>;
                    var createTask = hubActivator.CreateTask.Task;

                    // null ID means we're doing a Send and not an Invoke
                    await client.BeginUploadStreamAsync(invocationId: null, nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();
                    await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).OrTimeout();
                    await client.SendHubMessageAsync(new StreamItemMessage("id", " world")).OrTimeout();

                    await createTask.OrTimeout();
                    var tcs = hubActivator.ReleaseTask;
                    await client.SendHubMessageAsync(CompletionMessage.Empty("id")).OrTimeout();

                    await tcs.Task.OrTimeout();

                    // OnConnectedAsync and StreamingConcat hubs have been disposed
                    Assert.Equal(2, hubActivator.ReleaseCount);

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task UploadStreamClosesStreamsOnServerWhenMethodCompletes()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadIgnoreItems), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();

                    await client.SendHubMessageAsync(new StreamItemMessage("id", "ignored")).OrTimeout();
                    var result = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                    Assert.Null(simpleCompletion.Result);

                    // This will log a warning on the server as the hub method has completed and will complete all associated streams
                    await client.SendHubMessageAsync(new StreamItemMessage("id", "error!")).OrTimeout();

                    // Check that the connection hasn't been closed
                    await client.SendInvocationAsync("VoidMethod").OrTimeout();

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }

            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                w.EventId.Name == "ClosingStreamWithBindingError"));
        }

        [Fact]
        public async Task UploadStreamAndStreamingMethodClosesStreamsOnServerWhenMethodCompletes()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    await client.SendStreamInvocationAsync(nameof(MethodHub.StreamAndUploadIgnoreItems), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();

                    await client.SendHubMessageAsync(new StreamItemMessage("id", "ignored")).OrTimeout();
                    var result = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                    Assert.Null(simpleCompletion.Result);

                    // This will log a warning on the server as the hub method has completed and will complete all associated streams
                    await client.SendHubMessageAsync(new StreamItemMessage("id", "error!")).OrTimeout();

                    // Check that the connection hasn't been closed
                    await client.SendInvocationAsync("VoidMethod").OrTimeout();

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }

            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                w.EventId.Name == "ClosingStreamWithBindingError"));
        }

        [Theory]
        [InlineData(nameof(LongRunningHub.CancelableStreamSingleParameter))]
        [InlineData(nameof(LongRunningHub.CancelableStreamMultiParameter), 1, 2)]
        [InlineData(nameof(LongRunningHub.CancelableStreamMiddleParameter), 1, 2)]
        [InlineData(nameof(LongRunningHub.CancelableStreamGeneratedAsyncEnumerable))]
        [InlineData(nameof(LongRunningHub.CancelableStreamCustomAsyncEnumerable))]
        public async Task StreamHubMethodCanBeTriggeredOnCancellation(string methodName, params object[] args)
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var streamInvocationId = await client.SendStreamInvocationAsync(methodName, args).OrTimeout();
                    // Wait for the stream method to start
                    await tcsService.StartedMethod.Task.OrTimeout();

                    // Cancel the stream which should trigger the CancellationToken in the hub method
                    await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).OrTimeout();

                    var result = await client.ReadAsync().OrTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                    Assert.Null(simpleCompletion.Result);

                    // CancellationToken passed to hub method will allow EndMethod to be triggered if it is canceled.
                    await tcsService.EndMethod.Task.OrTimeout();

                    // Shut down
                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task StreamHubMethodCanAcceptCancellationTokenAsArgumentAndBeTriggeredOnConnectionAborted()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.CancelableStreamSingleParameter)).OrTimeout();
                    // Wait for the stream method to start
                    await tcsService.StartedMethod.Task.OrTimeout();

                    // Shut down the client which should trigger the CancellationToken in the hub method
                    client.Dispose();

                    // CancellationToken passed to hub method will allow EndMethod to be triggered if it is canceled.
                    await tcsService.EndMethod.Task.OrTimeout();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task StreamHubMethodCanAcceptNullableParameter()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.StreamNullableParameter), 5, null).OrTimeout();
                    // Wait for the stream method to start
                    var firstArgument = await tcsService.StartedMethod.Task.OrTimeout();
                    Assert.Equal(5, firstArgument);

                    var secondArgument = await tcsService.EndMethod.Task.OrTimeout();
                    Assert.Null(secondArgument);
                }
            }
        }


        [Fact]
        public async Task StreamHubMethodCanAcceptNullableParameterWithCancellationToken()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
                {
                    builder.AddSingleton(tcsService);
                }, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.CancelableStreamNullableParameter), 5, null).OrTimeout();
                    // Wait for the stream method to start
                    var firstArgument = await tcsService.StartedMethod.Task.OrTimeout();
                    Assert.Equal(5, firstArgument);

                    // Cancel the stream which should trigger the CancellationToken in the hub method
                    await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).OrTimeout();

                    var secondArgument = await tcsService.EndMethod.Task.OrTimeout();
                    Assert.Null(secondArgument);
                }
            }
        }

        [Fact]
        public async Task InvokeHubMethodCannotAcceptCancellationTokenAsArgument()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler).OrTimeout();

                    var invocationId = await client.SendInvocationAsync(nameof(MethodHub.InvalidArgument)).OrTimeout();

                    var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().OrTimeout());

                    Assert.Equal("Failed to invoke 'InvalidArgument' due to an error on the server.", completion.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CanPassStreamingParameterToStreamHubMethod()
        {
            using (StartVerifiableLog())
            {
                IServiceProvider serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);
                HubConnectionHandler<StreamingHub> connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
                Mock<IInvocationBinder> invocationBinder = new Mock<IInvocationBinder>();
                invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(string));

                using (TestClient client = new TestClient(invocationBinder: invocationBinder.Object))
                {
                    Task connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    // Wait for a connection, or for the endpoint to fail.
                    await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                    var streamId = "sample_id";
                    var messagePromise = client.StreamAsync(nameof(StreamingHub.StreamEcho), new[] { streamId }, Array.Empty<object>()).OrTimeout();

                    var phrases = new[] { "asdf", "qwer", "zxcv" };
                    foreach (var phrase in phrases)
                    {
                        await client.SendHubMessageAsync(new StreamItemMessage(streamId, phrase));
                    }
                    await client.SendHubMessageAsync(CompletionMessage.Empty(streamId));

                    var messages = await messagePromise;

                    // add one because this includes the completion
                    Assert.Equal(phrases.Count() + 1, messages.Count);
                    for (var i = 0; i < phrases.Count(); i++)
                    {
                        Assert.Equal("echo:" + phrases[i], ((StreamItemMessage)messages[i]).Item);
                    }

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        /// <summary>
        /// Hub methods might be written by users in a way that accepts an interface or base class as a parameter
        /// and deserialization could supply a derived class.
        /// This test ensures implementation and subclass arguments are correctly bound for dispatch.
        /// </summary>
        [Theory]
        [InlineData(nameof(StreamingHub.DerivedParameterInterfaceAsyncEnumerable))]
        [InlineData(nameof(StreamingHub.DerivedParameterBaseClassAsyncEnumerable))]
        [InlineData(nameof(StreamingHub.DerivedParameterInterfaceAsyncEnumerableWithCancellation))]
        [InlineData(nameof(StreamingHub.DerivedParameterBaseClassAsyncEnumerableWithCancellation))]
        public async Task CanPassDerivedParameterToStreamHubMethod(string method)
        {
            using (StartVerifiableLog())
            {
                var argument = new StreamingHub.DerivedParameterTestObject { Value = "test" };
                var protocolOptions = new NewtonsoftJsonHubProtocolOptions
                {
                    PayloadSerializerSettings = new JsonSerializerSettings()
                    {
                        // The usage of TypeNameHandling.All is a security risk.
                        // If you're implementing this in your own application instead use your own 'type' field and a custom JsonConverter
                        // or ensure you're restricting to only known types with a custom SerializationBinder like we are here.
                        // See https://github.com/dotnet/aspnetcore/issues/11495#issuecomment-505047422
                        TypeNameHandling = TypeNameHandling.All,
                        SerializationBinder = StreamingHub.DerivedParameterKnownTypesBinder.Instance
                    }
                };
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                    services => services.AddSignalR()
                        .AddNewtonsoftJsonProtocol(o => o.PayloadSerializerSettings = protocolOptions.PayloadSerializerSettings),
                    LoggerFactory);
                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
                var invocationBinder = new Mock<IInvocationBinder>();
                invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(string));

                using (var client = new TestClient(
                    protocol: new NewtonsoftJsonHubProtocol(Options.Create(protocolOptions)),
                    invocationBinder: invocationBinder.Object))
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    // Wait for a connection, or for the endpoint to fail.
                    await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                    var messages = await client.StreamAsync(method, argument).OrTimeout();

                    Assert.Equal(2, messages.Count);
                    HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, argument.Value), messages[0]);
                    HubConnectionHandlerTestUtils.AssertHubMessage(CompletionMessage.Empty(string.Empty), messages[1]);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task ClientsCallerPropertyCanBeUsedOutsideOfHub()
        {
            CallerService callerService = new CallerService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(callerService);
            });
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<CallerServiceHub>>();

            using (StartVerifiableLog())
            {
                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    // Wait for a connection, or for the endpoint to fail.
                    await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                    await callerService.Caller.SendAsync("Echo", "message").OrTimeout();

                    var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout());

                    Assert.Equal("Echo", message.Target);
                    Assert.Equal("message", message.Arguments[0]);
                }
            }
        }

        [Fact]
        public async Task ConnectionCloseCleansUploadStreams()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider();
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (StartVerifiableLog())
            {
                using var client = new TestClient();

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Wait for a connection, or for the endpoint to fail.
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadDoesWorkOnComplete), streamIds: new[] { "id" }, args: Array.Empty<object>()).OrTimeout();

                await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).OrTimeout();

                await client.DisposeAsync().OrTimeout();

                await connectionHandlerTask.OrTimeout();

                // This task completes if the upload stream is completed, via closing the connection
                var task = (Task<int>)client.Connection.Items[nameof(MethodHub.UploadDoesWorkOnComplete)];

                var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => task).OrTimeout();
                Assert.Equal("The underlying connection was closed.", exception.Message);
            }
        }

        [Fact]
        public async Task SpecificHubOptionForMaximumReceiveMessageSizeIsUsedOverGlobalHubOption()
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(serviceBuilder =>
            {
                serviceBuilder.AddSignalR(o =>
                {
                    // ConnectAsync would fail if this value was used
                    o.MaximumReceiveMessageSize = 1;
                }).AddHubOptions<MethodHub>(o =>
                {
                    // null is treated as both no-limit and not set, this test verifies that we track if the user explicitly sets the value
                    o.MaximumReceiveMessageSize = null;
                });
            });
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (StartVerifiableLog())
            {
                using var client = new TestClient();

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Wait for a connection, or for the endpoint to fail.
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).OrTimeout();

                await client.DisposeAsync().OrTimeout();

                await connectionHandlerTask.OrTimeout();
            }
        }

        private class CustomHubActivator<THub> : IHubActivator<THub> where THub : Hub
        {
            public int ReleaseCount;
            private IServiceProvider _serviceProvider;
            public TaskCompletionSource<object> ReleaseTask = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> CreateTask = new TaskCompletionSource<object>();

            public CustomHubActivator(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public THub Create()
            {
                ReleaseTask = new TaskCompletionSource<object>();
                var hub = new DefaultHubActivator<THub>(_serviceProvider).Create();
                CreateTask.TrySetResult(null);
                return hub;
            }

            public void Release(THub hub)
            {
                ReleaseCount++;
                hub.Dispose();
                ReleaseTask.TrySetResult(null);
                CreateTask = new TaskCompletionSource<object>();
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

        private class TestUserIdProvider : IUserIdProvider
        {
            private readonly Func<HubConnectionContext, string> _getUserId;

            public TestUserIdProvider(Func<HubConnectionContext, string> getUserId)
            {
                _getUserId = getUserId;
            }

            public string GetUserId(HubConnectionContext connection) => _getUserId(connection);
        }

        private class SampleObject
        {
            public SampleObject(string foo, int bar)
            {
                Bar = bar;
                Foo = foo;
            }
            public int Bar { get; }
            public string Foo { get; }
        }

        private class HttpContextFeatureImpl : IHttpContextFeature
        {
            public HttpContext HttpContext { get; set; }
        }
    }
}

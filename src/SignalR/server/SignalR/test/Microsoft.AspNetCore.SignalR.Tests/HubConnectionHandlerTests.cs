// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests : VerifiableLoggedTest
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

                var result = (await client.InvokeAsync(nameof(HubWithAsyncDisposable.Test)).DefaultTimeout());
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

                await connectionHandlerTask.DefaultTimeout();

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

                await connectionHandlerTask.DefaultTimeout();

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

                await client.SendInvocationAsync(nameof(AbortHub.Kill)).DefaultTimeout();

                var close = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.False(close.AllowReconnect);

                await connectionHandlerTask.DefaultTimeout();

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

                await connectionHandlerTask.DefaultTimeout();
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
            Assert.Equal($"Cannot generate proxy implementation for '{typeof(IVoidReturningTypedHubClient).FullName}.{nameof(IVoidReturningTypedHubClient.Send)}'. All client proxy methods must return '{typeof(Task).FullName}' or 'System.Threading.Tasks.Task<T>'.", ex.Message);
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

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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

                var message = await client.ReadAsync(isHandshake: true).DefaultTimeout();

                Assert.Equal("Handshake was canceled.", ((HandshakeResponseMessage)message).Error);

                // Connection closes
                await connectionHandlerTask.DefaultTimeout();

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

                await connectionHandlerTask.DefaultTimeout();
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

                var completionMessage = await task.DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("hello", completionMessage.Result);
                Assert.Equal("1", completionMessage.InvocationId);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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
                var completionMessage = await client.ReadAsync().DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("1", completionMessage.InvocationId);
                Assert.Equal("one", completionMessage.Result);

                completionMessage = await client.ReadAsync().DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("2", completionMessage.InvocationId);
                Assert.Equal("two", completionMessage.Result);

                // We never receive the 3rd message since it was over the maximum message size
                CloseMessage closeMessage = await client.ReadAsync().DefaultTimeout() as CloseMessage;
                Assert.NotNull(closeMessage);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var completionMessage = await client.ReadAsync().DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("1", completionMessage.InvocationId);
                Assert.Equal("one", completionMessage.Result);

                completionMessage = await client.ReadAsync().DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("2", completionMessage.InvocationId);
                Assert.Equal("two", completionMessage.Result);

                completionMessage = await client.ReadAsync().DefaultTimeout() as CompletionMessage;
                Assert.NotNull(completionMessage);
                Assert.Equal("3", completionMessage.InvocationId);
                Assert.Equal("three", completionMessage.Result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();
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
                await client.Connection.Application.Output.WriteAsync(payload).DefaultTimeout();

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler, sendHandshakeRequestMessage: false, expectedHandshakeResponseMessage: false);
                // Complete the pipe to 'close' the connection
                client.Connection.Application.Output.Complete();

                // This will never complete as the pipe was completed and nothing can be written to it
                var handshakeReadTask = client.ReadAsync(true);

                // Check that the connection was closed on the server
                await connectionHandlerTask.DefaultTimeout();
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
                            await connectionHandlerTask.DefaultTimeout();
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

                await connectionHandlerTask.DefaultTimeout();

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

                var result = (await client.InvokeAsync(nameof(MethodHub.TaskValueMethod)).DefaultTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(42L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(MethodHub.ValueTaskValueMethod)).DefaultTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(43L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(MethodHub.ValueTaskMethod)).DefaultTimeout()).Result;

                Assert.Null(result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync("echo", "hello").DefaultTimeout()).Result;

                Assert.Equal("hello", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var message = await client.InvokeAsync(methodName).DefaultTimeout();

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

                await connectionHandlerTask.DefaultTimeout();
            }
        }

        Assert.True(hasErrorLog);
    }

    [Fact]
    public async Task HubMethodListeningToConnectionAbortedClosesOnConnectionContextAbort()
    {
        using (StartVerifiableLog())
        {
            var connectionHandler = HubConnectionHandlerTestUtils.GetHubConnectionHandler(typeof(MethodHub), loggerFactory: LoggerFactory);

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.SendInvocationAsync(nameof(MethodHub.BlockingMethod)).DefaultTimeout();

                client.Connection.Abort();

                var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.False(closeMessage.AllowReconnect);

                // If this completes then the server has completed the connection
                await connectionHandlerTask.DefaultTimeout();

                // Nothing written to connection because it was closed
                Assert.Null(client.TryRead());
            }
        }
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

                var message = await client.InvokeAsync(methodName).DefaultTimeout();

                Assert.Equal($"An unexpected error occurred invoking '{methodName}' on the server. HubException: This is a hub exception", message.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.SendInvocationAsync(nameof(MethodHub.ValueMethod), nonBlocking: true).DefaultTimeout();

                // kill the connection
                client.Dispose();

                var message = Assert.IsType<CloseMessage>(client.TryRead());
                Assert.True(message.AllowReconnect);

                // Ensure the client channel is empty
                Assert.Null(client.TryRead());

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(MethodHub.VoidMethod)).DefaultTimeout()).Result;

                Assert.Null(result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync("RenamedMethod").DefaultTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(43L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync("RenamedVirtualMethod").DefaultTimeout()).Result;

                // json serializer makes this a long
                Assert.Equal(34L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await client.SendInvocationAsync(methodName, nonBlocking: true).DefaultTimeout();

                // kill the connection
                client.Dispose();

                // only thing written should be close message
                var closeMessage = await client.ReadAsync().DefaultTimeout();
                Assert.IsType<CloseMessage>(closeMessage);

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(MethodHub.ConcatString), (byte)32, 42, 'm', "string").DefaultTimeout()).Result;

                Assert.Equal("32, 42, m, string", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(InheritedHub.BaseMethod), "string").DefaultTimeout()).Result;

                Assert.Equal("string", result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = (await client.InvokeAsync(nameof(InheritedHub.VirtualMethod), 10).DefaultTimeout()).Result;

                Assert.Equal(0L, result);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = await client.InvokeAsync(nameof(MethodHub.OnDisconnectedAsync)).DefaultTimeout();

                Assert.Equal("Failed to invoke 'OnDisconnectedAsync' due to an error on the server. HubException: Method does not exist.", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = await client.InvokeAsync(nameof(MethodHub.StaticMethod)).DefaultTimeout();

                Assert.Equal("Failed to invoke 'StaticMethod' due to an error on the server. HubException: Method does not exist.", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = await client.InvokeAsync(nameof(MethodHub.ToString)).DefaultTimeout();
                Assert.Equal("Failed to invoke 'ToString' due to an error on the server. HubException: Method does not exist.", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.GetHashCode)).DefaultTimeout();
                Assert.Equal("Failed to invoke 'GetHashCode' due to an error on the server. HubException: Method does not exist.", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.Equals)).DefaultTimeout();
                Assert.Equal("Failed to invoke 'Equals' due to an error on the server. HubException: Method does not exist.", result.Error);

                result = await client.InvokeAsync(nameof(MethodHub.ReferenceEquals)).DefaultTimeout();
                Assert.Equal("Failed to invoke 'ReferenceEquals' due to an error on the server. HubException: Method does not exist.", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                var result = await client.InvokeAsync(nameof(MethodHub.Dispose)).DefaultTimeout();

                Assert.Equal("Failed to invoke 'Dispose' due to an error on the server. HubException: Method does not exist.", result.Error);

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.BroadcastMethod), "test").DefaultTimeout();

                foreach (var result in await Task.WhenAll(
                    firstClient.ReadAsync(),
                    secondClient.ReadAsync()).DefaultTimeout())
                {
                    var invocation = Assert.IsType<InvocationMessage>(result);
                    Assert.Equal("Broadcast", invocation.Target);
                    Assert.Single(invocation.Arguments);
                    Assert.Equal("test", invocation.Arguments[0]);
                }

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.SendArray)).DefaultTimeout();

                foreach (var result in await Task.WhenAll(
                    firstClient.ReadAsync(),
                    secondClient.ReadAsync()).DefaultTimeout())
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

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync("SendToOthers", "To others").DefaultTimeout();

                var secondClientResult = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To others", invocation.Arguments[0]);

                var firstClientResult = await firstClient.ReadAsync().DefaultTimeout();
                var completion = Assert.IsType<CompletionMessage>(firstClientResult);

                await secondClient.SendInvocationAsync("BroadcastMethod", "To everyone").DefaultTimeout();
                firstClientResult = await firstClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(firstClientResult);
                Assert.Equal("Broadcast", invocation.Target);
                Assert.Equal("To everyone", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync("SendToCaller", "To caller").DefaultTimeout();

                var firstClientResult = await firstClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(firstClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To caller", invocation.Arguments[0]);

                await firstClient.SendInvocationAsync("BroadcastMethod", "To everyone").DefaultTimeout();
                var secondClientResult = await secondClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Broadcast", invocation.Target);
                Assert.Equal("To everyone", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).DefaultTimeout();

                var excludeSecondClientId = new HashSet<string>();
                excludeSecondClientId.Add(secondClient.Connection.ConnectionId);
                var excludeThirdClientId = new HashSet<string>();
                excludeThirdClientId.Add(thirdClient.Connection.ConnectionId);

                await firstClient.SendInvocationAsync("SendToAllExcept", "To second", excludeThirdClientId).DefaultTimeout();
                await firstClient.SendInvocationAsync("SendToAllExcept", "To third", excludeSecondClientId).DefaultTimeout();

                var secondClientResult = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To second", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("To third", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).DefaultTimeout();

                var secondAndThirdClients = new HashSet<string> {secondClient.Connection.ConnectionId,
                    thirdClient.Connection.ConnectionId };

                await firstClient.SendInvocationAsync("SendToMultipleClients", "Second and Third", secondAndThirdClients).DefaultTimeout();

                var secondClientResult = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                // Check that first client only got the completion message
                var hubMessage = await firstClient.ReadAsync().DefaultTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);
                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected, thirdClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync(nameof(MethodHub.SendToMultipleUsers), new[] { "userB", "userC" }, "Second and Third").DefaultTimeout();

                var secondClientResult = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(secondClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                var thirdClientResult = await thirdClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(thirdClientResult);
                Assert.Equal("Send", invocation.Target);
                Assert.Equal("Second and Third", invocation.Arguments[0]);

                // Check that first client only got the completion message
                var hubMessage = await firstClient.ReadAsync().DefaultTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);
                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();
                thirdClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask, thirdConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").DefaultTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                result = (await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").DefaultTimeout()).Result;

                await firstClient.SendInvocationAsync(nameof(MethodHub.GroupSendMethod), "testGroup", "test").DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").DefaultTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").DefaultTimeout();
                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").DefaultTimeout();

                var excludedConnectionIds = new List<string> { firstClient.Connection.ConnectionId };

                await firstClient.SendInvocationAsync("GroupExceptSendMethod", "testGroup", "test", excludedConnectionIds).DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // Check that first client only got the completion message
                hubMessage = await firstClient.ReadAsync().DefaultTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);

                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                var result = (await firstClient.InvokeAsync("GroupSendMethod", "testGroup", "test").DefaultTimeout()).Result;

                // check that 'firstConnection' hasn't received the group send
                Assert.Null(firstClient.TryRead());

                // check that 'secondConnection' hasn't received the group send
                Assert.Null(secondClient.TryRead());

                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").DefaultTimeout();
                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "testGroup").DefaultTimeout();

                await firstClient.SendInvocationAsync("SendToOthersInGroup", "testGroup", "test").DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // Check that first client only got the completion message
                hubMessage = await firstClient.ReadAsync().DefaultTimeout();
                Assert.IsType<CompletionMessage>(hubMessage);

                Assert.Null(firstClient.TryRead());

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await secondClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "GroupA").DefaultTimeout();
                await firstClient.InvokeAsync(nameof(MethodHub.GroupAddMethod), "GroupB").DefaultTimeout();

                var groupNames = new List<string> { "GroupA", "GroupB" };
                await firstClient.SendInvocationAsync(nameof(MethodHub.SendToMultipleGroups), "test", groupNames).DefaultTimeout();

                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                hubMessage = await firstClient.ReadAsync().DefaultTimeout();
                invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await client.SendInvocationAsync(nameof(MethodHub.GroupRemoveMethod), "testGroup").DefaultTimeout();

                // kill the connection
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync("ClientSendMethod", "userB", "test").DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync("ConnectionSendMethod", secondClient.Connection.ConnectionId, "test").DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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

                await Task.WhenAll(firstClient.Connected, secondClient.Connected).DefaultTimeout();

                await firstClient.SendInvocationAsync("DelayedSend", secondClient.Connection.ConnectionId, "test").DefaultTimeout();

                // check that 'secondConnection' has received the group send
                var hubMessage = await secondClient.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(hubMessage);
                Assert.Equal("Send", invocation.Target);
                Assert.Single(invocation.Arguments);
                Assert.Equal("test", invocation.Arguments[0]);

                // kill the connections
                firstClient.Dispose();
                secondClient.Dispose();

                await Task.WhenAll(firstConnectionHandlerTask, secondConnectionHandlerTask).DefaultTimeout();
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
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

                var messages = await client.StreamAsync(method, 4).DefaultTimeout();

                Assert.Equal(5, messages.Count);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "0"), messages[0]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "1"), messages[1]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "2"), messages[2]);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, "3"), messages[3]);
                HubConnectionHandlerTestUtils.AssertHubMessage(CompletionMessage.Empty(string.Empty), messages[4]);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                var invocationId = Guid.NewGuid().ToString("N");
                await client.SendHubMessageAsync(new StreamInvocationMessage(invocationId, nameof(StreamingHub.BlockingStream), Array.Empty<object>()));

                // cancel the Streaming method
                await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).DefaultTimeout();

                var hubMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(invocationId, hubMessage.InvocationId);
                Assert.Null(hubMessage.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Theory]
    [InlineData(nameof(StreamingHub.ExceptionAsyncEnumerable), "Exception: Exception from async enumerable")]
    [InlineData(nameof(StreamingHub.ExceptionAsyncEnumerable), null)]
    [InlineData(nameof(StreamingHub.ExceptionStream), "Exception: Exception from channel")]
    [InlineData(nameof(StreamingHub.ExceptionStream), null)]
    [InlineData(nameof(StreamingHub.ChannelClosedExceptionStream), "ChannelClosedException: ChannelClosedException from channel")]
    [InlineData(nameof(StreamingHub.ChannelClosedExceptionStream), null)]
    [InlineData(nameof(StreamingHub.ChannelClosedExceptionInnerExceptionStream), "Exception: ChannelClosedException from channel")]
    [InlineData(nameof(StreamingHub.ChannelClosedExceptionInnerExceptionStream), null)]
    public async Task ReceiveCorrectErrorFromStreamThrowing(string streamMethod, string detailedError)
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                   writeContext.EventId.Name == "FailedStreaming";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            builder.AddSignalR(options =>
            {
                options.EnableDetailedErrors = detailedError != null;
            }), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                var messages = await client.StreamAsync(streamMethod);

                Assert.Equal(1, messages.Count);
                var completion = messages[0] as CompletionMessage;
                Assert.NotNull(completion);
                if (detailedError != null)
                {
                    Assert.Equal($"An error occurred on the server while streaming results. {detailedError}", completion.Error);
                }
                else
                {
                    Assert.Equal("An error occurred on the server while streaming results.", completion.Error);
                }

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client1.Connected.DefaultTimeout();
                await client2.Connected.DefaultTimeout();

                var sentMessage = "From Json";

                await client1.SendInvocationAsync(nameof(MethodHub.BroadcastMethod), sentMessage);
                var message1 = await client1.ReadAsync().DefaultTimeout();
                var message2 = await client2.ReadAsync().DefaultTimeout();

                var completion1 = message1 as InvocationMessage;
                Assert.NotNull(completion1);
                Assert.Equal(sentMessage, completion1.Arguments[0]);
                var completion2 = message2 as InvocationMessage;
                Assert.NotNull(completion2);
                // Argument[0] is a 'MsgPackObject' with a string internally, ToString to compare it
                Assert.Equal(sentMessage, completion2.Arguments[0].ToString());

                client1.Dispose();
                client2.Dispose();

                await firstConnectionHandlerTask.DefaultTimeout();
                await secondConnectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).DefaultTimeout();

                Assert.NotNull(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.AuthMethod)).DefaultTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    private class TestConnectionLifetimeNotification : IConnectionLifetimeNotificationFeature
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public CancellationToken ConnectionClosedRequested { get => _cts.Token; set => throw new NotImplementedException(); }

        public void RequestClose()
        {
            _cts.Cancel();
        }
    }

    [Fact]
    public async Task ConnectionLifetimeNotificationClosesConnectionWithReconnectAllowed()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(loggerFactory: LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                client.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(new TestConnectionLifetimeNotification());

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                client.Connection.Features.Get<IConnectionLifetimeNotificationFeature>().RequestClose();

                var close = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());

                Assert.True(close.AllowReconnect);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    private class TestAuthHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            Assert.NotNull(context.Resource);
            var resource = Assert.IsType<HubInvocationContext>(context.Resource);
            Assert.Equal(typeof(MethodHub), resource.Hub.GetType());
            Assert.Equal(nameof(MethodHub.MultiParamAuthMethod), resource.HubMethodName);
            Assert.Equal(2, resource.HubMethodArguments?.Count);
            Assert.Equal("Hello", resource.HubMethodArguments[0]);
            Assert.Equal("World!", resource.HubMethodArguments[1]);
            Assert.NotNull(resource.Context);
            Assert.Equal(context.User, resource.Context.User);
            Assert.NotNull(resource.Context.GetHttpContext());
            Assert.NotNull(resource.ServiceProvider);
            Assert.Equal(typeof(MethodHub).GetMethod(nameof(MethodHub.MultiParamAuthMethod)), resource.HubMethod);

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

                await client.Connected.DefaultTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "Hello", "World!").DefaultTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).DefaultTimeout();

                var message = (InvocationMessage)await client.ReadAsync().DefaultTimeout();

                var customItem = message.Arguments[0].ToString();
                // by default properties serialized by JsonHubProtocol are using camelCasing
                Assert.Contains("Message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).DefaultTimeout();

                var message = (InvocationMessage)await client.ReadAsync().DefaultTimeout();

                var customItem = message.Arguments[0].ToString();
                // originally Message, paramName
                Assert.Contains("message", customItem);
                Assert.Contains("paramName", customItem);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                        options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(new CustomFormatter(), options.SerializerOptions.Resolver));
                    });
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            var msgPackOptions = serviceProvider.GetRequiredService<IOptions<MessagePackHubProtocolOptions>>();
            using (var client = new TestClient(protocol: new MessagePackHubProtocol(msgPackOptions)))
            {
                client.SupportedFormats = TransferFormat.Binary;
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.BroadcastItem)).DefaultTimeout();

                var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());

                var result = message.Arguments[0] as Dictionary<object, object>;
                Assert.Equal("formattedString", result["Message"]);
                Assert.Equal("formattedString", result["paramName"]);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await await client.ConnectAsync(connectionHandler, expectedHandshakeResponseMessage: false)).DefaultTimeout();
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
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await await client.ConnectAsync(connectionHandler, expectedHandshakeResponseMessage: false)).DefaultTimeout();
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

                await client1.Connected.DefaultTimeout();
                await client2.Connected.DefaultTimeout();

                await client2.SendInvocationAsync(nameof(MethodHub.SendToMultipleUsers), new[] { "client1" }, "Hi!").DefaultTimeout();

                var message = (InvocationMessage)await client1.ReadAsync().DefaultTimeout();

                Assert.Equal("Send", message.Target);
                Assert.Collection(message.Arguments, arg => Assert.Equal("Hi!", arg));

                client1.Dispose();
                client2.Dispose();

                await connectionHandlerTask1.DefaultTimeout();
                await connectionHandlerTask2.DefaultTimeout();

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
                return (T)(object)reader.ReadString();
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

                await client.Connected.DefaultTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).DefaultTimeout()).Result;
                Assert.True((bool)result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                var result = (await client.InvokeAsync(nameof(MethodHub.HasHttpContext)).DefaultTimeout()).Result;
                Assert.False((bool)result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await client.Connected.DefaultTimeout();

                // Send a ping
                await client.SendHubMessageAsync(PingMessage.Instance).DefaultTimeout();

                // Now do an invocation to make sure we processed the ping message
                var completion = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                Assert.NotNull(completion);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                // Echo a bunch of stuff, waiting 10ms between each, until 500ms have elapsed
                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalMilliseconds <= 500.0)
                {
                    await client.SendInvocationAsync("Echo", "foo").DefaultTimeout();
                    await Task.Delay(10);
                }

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

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
            var interval = TimeSpan.FromMilliseconds(100);
            var timeProvider = new FakeTimeProvider();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.KeepAliveInterval = interval), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();
            connectionHandler.TimeProvider = timeProvider;

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.DefaultTimeout();

                // Trigger multiple keep alives
                var heartbeatCount = 5;
                for (var i = 0; i < heartbeatCount; i++)
                {
                    timeProvider.Advance(interval + TimeSpan.FromMilliseconds(1));
                    client.TickHeartbeat();
                }

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                client.Connection.Transport.Output.Complete();

                // We should have all pings (and close message)
                HubMessage message;
                var pingCounter = 0;
                var hasCloseMessage = false;
                while ((message = await client.ReadAsync().DefaultTimeout()) != null)
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
                Assert.Equal(heartbeatCount, pingCounter);
            }
        }
    }

    [Fact]
    public async Task ConnectionNotTimedOutIfClientNeverPings()
    {
        using (StartVerifiableLog())
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var timeProvider = new FakeTimeProvider();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.ClientTimeoutInterval = timeout), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();
            connectionHandler.TimeProvider = timeProvider;

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.DefaultTimeout();
                // This is a fake client -- it doesn't auto-ping to signal

                // We go over the 100 ms timeout interval multiple times
                for (var i = 0; i < 3; i++)
                {
                    timeProvider.Advance(timeout + TimeSpan.FromMilliseconds(1));
                    client.TickHeartbeat();
                }

                // Invoke a Hub method and wait for the result to reliably test if the connection is still active
                var id = await client.SendInvocationAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                var result = await client.ReadAsync().DefaultTimeout();

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
            var timeout = TimeSpan.FromMilliseconds(100);
            var timeProvider = new FakeTimeProvider();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.ClientTimeoutInterval = timeout), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();
            connectionHandler.TimeProvider = timeProvider;

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.DefaultTimeout();
                await client.SendHubMessageAsync(PingMessage.Instance);

                timeProvider.Advance(timeout + TimeSpan.FromMilliseconds(1));
                client.TickHeartbeat();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task OnDisconnectedAsyncReceivesExceptionOnPingTimeout()
    {
        using (StartVerifiableLog())
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var timeProvider = new FakeTimeProvider();
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.Configure<HubOptions>(options =>
                    options.ClientTimeoutInterval = timeout);

                services.AddSingleton(state);
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();
            connectionHandler.TimeProvider = timeProvider;

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.SendHubMessageAsync(PingMessage.Instance);

                timeProvider.Advance(timeout + TimeSpan.FromMilliseconds(1));
                client.TickHeartbeat();

                await connectionHandlerTask.DefaultTimeout();

                var ex = Assert.IsType<OperationCanceledException>(state.DisconnectedException);
                Assert.Equal("Client hasn't sent a message/ping within the configured ClientTimeoutInterval.", ex.Message);
            }
        }
    }

    [Fact]
    public async Task ReceivingMessagesPreventsConnectionTimeoutFromOccuring()
    {
        using (StartVerifiableLog())
        {
            var timeout = TimeSpan.FromMilliseconds(300);
            var timeProvider = new FakeTimeProvider();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                services.Configure<HubOptions>(options =>
                    options.ClientTimeoutInterval = timeout), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();
            connectionHandler.TimeProvider = timeProvider;

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                await client.Connected.DefaultTimeout();
                await client.SendHubMessageAsync(PingMessage.Instance);

                for (int i = 0; i < 10; i++)
                {
                    timeProvider.Advance(timeout - TimeSpan.FromMilliseconds(1));
                    client.TickHeartbeat();
                    await client.SendHubMessageAsync(PingMessage.Instance);
                }

                // Invoke a Hub method and wait for the result to reliably test if the connection is still active
                var id = await client.SendInvocationAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                var result = await client.ReadAsync().DefaultTimeout();

                Assert.IsType<CompletionMessage>(result);

                Assert.False(connectionHandlerTask.IsCompleted);
            }
        }
    }

    internal class PipeReaderWrapper : PipeReader
    {
        private readonly PipeReader _originalPipeReader;
        private TaskCompletionSource _waitForRead;
        private readonly object _lock = new object();

        public PipeReaderWrapper(PipeReader pipeReader)
        {
            _originalPipeReader = pipeReader;
            _waitForRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
                _waitForRead.SetResult();
            }

            try
            {
                return await _originalPipeReader.ReadAsync(cancellationToken);
            }
            finally
            {
                lock (_lock)
                {
                    _waitForRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
                {
                    options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(0);
                    options.MaximumParallelInvocationsPerClient = 1;
                });
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
                var hubMethodTask1 = client.InvokeAsync(nameof(LongRunningHub.LongRunningMethod));
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Wait for server to start reading again
                await customDuplex.WrappedPipeReader.WaitForReadStart().DefaultTimeout();
                // Send another invocation to server, since we use Inline scheduling we know that once this call completes the server will have read and processed
                // the message, it should be stuck waiting for the in-progress invoke now
                _ = await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod)).DefaultTimeout();

                // Tick heartbeat while hub method is running to show that close isn't triggered
                client.TickHeartbeat();

                // Unblock long running hub method
                tcsService.EndMethod.SetResult(null);

                await hubMethodTask1.DefaultTimeout();
                await client.ReadAsync().DefaultTimeout();

                // There is a small window when the hub method finishes and the timer starts again
                // So we need to delay a little before ticking the heart beat.
                // We do this by waiting until we know the HubConnectionHandler code is in pipe.ReadAsync()
                await customDuplex.WrappedPipeReader.WaitForReadStart().DefaultTimeout();

                // Tick heartbeat again now that we're outside of the hub method
                client.TickHeartbeat();

                // Connection is closed
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task HubMethodInvokeCountsTowardsClientTimeoutIfParallelNotMaxed()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.Configure<HubOptions>(options =>
                {
                    options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(0);
                    options.MaximumParallelInvocationsPerClient = 2;
                });
                services.AddSingleton(tcsService);
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

            using (var client = new TestClient(new JsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
                // This starts the timeout logic
                await client.SendHubMessageAsync(PingMessage.Instance);

                // Call long running hub method
                var hubMethodTask = client.InvokeAsync(nameof(LongRunningHub.LongRunningMethod));
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Tick heartbeat while hub method is running
                client.TickHeartbeat();

                // Connection is closed
                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                client.Connection.Transport.Output.Complete();

                var message = await client.ReadAsync().DefaultTimeout();

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

                var message = await client.ReadAsync().DefaultTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(message);
                if (detailedErrors)
                {
                    Assert.Equal("Connection closed with an error. InvalidOperationException: Hub OnConnected failed.", closeMessage.Error);
                }
                else
                {
                    Assert.Equal("Connection closed with an error.", closeMessage.Error);
                }

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task StreamingInvocationsDoNotBlockOtherInvocations()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.MaximumParallelInvocationsPerClient = 1;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient(new NewtonsoftJsonHubProtocol()))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Blocking streaming invocation to test that other invocations can still run
                await client.SendHubMessageAsync(new StreamInvocationMessage("1", nameof(StreamingHub.BlockingStream), Array.Empty<object>())).DefaultTimeout();

                var completion = await client.InvokeAsync(nameof(StreamingHub.NonStream)).DefaultTimeout();
                Assert.Equal(42L, completion.Result);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task StreamMethodThatThrowsWillCleanup()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                   writeContext.EventId.Name == "FailedInvokingHubMethod";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                var messages = await client.StreamAsync(nameof(StreamingHub.ThrowStream));

                Assert.Equal(1, messages.Count);
                var completion = messages[0] as CompletionMessage;
                Assert.NotNull(completion);

                var hubActivator = serviceProvider.GetService<IHubActivator<StreamingHub>>() as CustomHubActivator<StreamingHub>;

                // OnConnectedAsync and ThrowStream hubs have been disposed
                Assert.Equal(2, hubActivator.ReleaseCount);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task StreamMethodThatReturnsNullWillCleanup()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                var messages = await client.StreamAsync(nameof(StreamingHub.NullStream));

                Assert.Equal(1, messages.Count);
                var completion = messages[0] as CompletionMessage;
                Assert.NotNull(completion);

                var hubActivator = serviceProvider.GetService<IHubActivator<StreamingHub>>() as CustomHubActivator<StreamingHub>;

                // OnConnectedAsync and NullStream hubs have been disposed
                Assert.Equal(2, hubActivator.ReleaseCount);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task StreamMethodWithDuplicateIdFails()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                await client.SendHubMessageAsync(new StreamInvocationMessage("123", nameof(StreamingHub.BlockingStream), Array.Empty<object>())).DefaultTimeout();

                await client.SendHubMessageAsync(new StreamInvocationMessage("123", nameof(StreamingHub.BlockingStream), Array.Empty<object>())).DefaultTimeout();

                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal("Invocation ID '123' is already in use.", completion.Error);

                var hubActivator = serviceProvider.GetService<IHubActivator<StreamingHub>>() as CustomHubActivator<StreamingHub>;

                // OnConnectedAsync and BlockingStream hubs have been disposed
                Assert.Equal(2, hubActivator.ReleaseCount);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task InvocationsRunInOrderWithNoParallelism()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);

                builder.AddSignalR(options =>
                {
                    options.MaximumParallelInvocationsPerClient = 1;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

            // Because we use PipeScheduler.Inline the hub invocations will run inline until they wait, which happens inside the LongRunningMethod call
            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Long running hub invocation to test that other invocations will not run until it is completed
                await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod), nonBlocking: false).DefaultTimeout();
                // Wait for the long running method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Invoke another hub method which will wait for the first method to finish
                await client.SendInvocationAsync(nameof(LongRunningHub.SimpleMethod), nonBlocking: false).DefaultTimeout();
                // Both invocations should be waiting now
                Assert.Null(client.TryRead());

                // Release the long running hub method
                tcsService.EndMethod.TrySetResult(null);

                // Long running hub method result
                var firstResult = await client.ReadAsync().DefaultTimeout();

                var longRunningCompletion = Assert.IsType<CompletionMessage>(firstResult);
                Assert.Equal(12L, longRunningCompletion.Result);

                // simple hub method result
                var secondResult = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(secondResult);
                Assert.Equal(21L, simpleCompletion.Result);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task InvocationsCanRunOutOfOrderWithParallelism()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);

                builder.AddSignalR(options =>
                {
                    options.MaximumParallelInvocationsPerClient = 2;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

            // Because we use PipeScheduler.Inline the hub invocations will run inline until they wait, which happens inside the LongRunningMethod call
            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Long running hub invocation to test that other invocations will not run until it is completed
                await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod), nonBlocking: false).DefaultTimeout();
                // Wait for the long running method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                for (var i = 0; i < 5; i++)
                {
                    // Invoke another hub method which will wait for the first method to finish
                    await client.SendInvocationAsync(nameof(LongRunningHub.SimpleMethod), nonBlocking: false).DefaultTimeout();

                    // simple hub method result
                    var secondResult = await client.ReadAsync().DefaultTimeout();

                    var simpleCompletion = Assert.IsType<CompletionMessage>(secondResult);
                    Assert.Equal(21L, simpleCompletion.Result);
                }

                // Release the long running hub method
                tcsService.EndMethod.TrySetResult(null);

                // Long running hub method result
                var firstResult = await client.ReadAsync().DefaultTimeout();

                var longRunningCompletion = Assert.IsType<CompletionMessage>(firstResult);
                Assert.Equal(12L, longRunningCompletion.Result);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task PendingInvocationUnblockedWhenBlockingMethodCompletesWithParallelism()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);

                builder.AddSignalR(options =>
                {
                    options.MaximumParallelInvocationsPerClient = 2;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

            // Because we use PipeScheduler.Inline the hub invocations will run inline until they wait, which happens inside the LongRunningMethod call
            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Long running hub invocation to test that other invocations will not run until it is completed
                await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod), nonBlocking: false).DefaultTimeout();
                // Wait for the long running method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();
                // Grab the tcs before resetting to use in the second long running method
                var endTcs = tcsService.EndMethod;
                tcsService.Reset();

                // Long running hub invocation to test that other invocations will not run until it is completed
                await client.SendInvocationAsync(nameof(LongRunningHub.LongRunningMethod), nonBlocking: false).DefaultTimeout();
                // Wait for the long running method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Invoke another hub method which will wait for the first method to finish
                await client.SendInvocationAsync(nameof(LongRunningHub.SimpleMethod), nonBlocking: false).DefaultTimeout();
                // Both invocations should be waiting now
                Assert.Null(client.TryRead());

                // Release the second long running hub method
                tcsService.EndMethod.TrySetResult(null);

                // Long running hub method result
                var firstResult = await client.ReadAsync().DefaultTimeout();

                var longRunningCompletion = Assert.IsType<CompletionMessage>(firstResult);
                Assert.Equal(12L, longRunningCompletion.Result);

                // simple hub method result
                var secondResult = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(secondResult);
                Assert.Equal(21L, simpleCompletion.Result);

                // Release the first long running hub method
                endTcs.TrySetResult(null);

                firstResult = await client.ReadAsync().DefaultTimeout();
                longRunningCompletion = Assert.IsType<CompletionMessage>(firstResult);
                Assert.Equal(12L, longRunningCompletion.Result);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task StreamInvocationsDoNotBlockOtherInvocations()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);
                builder.AddSingleton(typeof(IHubActivator<>), typeof(CustomHubActivator<>));

                builder.AddSignalR(options =>
                {
                    options.MaximumParallelInvocationsPerClient = 1;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

            // Because we use PipeScheduler.Inline the hub invocations will run inline until they go async
            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Long running stream invocation to test that other invocations can still run before it is completed
                var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.LongRunningStream), null).DefaultTimeout();
                // Wait for the long running method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Invoke another hub method which will be able to run even though a streaming method is still running
                var completion = await client.InvokeAsync(nameof(LongRunningHub.SimpleMethod)).DefaultTimeout();
                Assert.Null(completion.Error);
                Assert.Equal(21L, completion.Result);

                // Release the long running hub method
                tcsService.EndMethod.TrySetResult(null);

                var hubActivator = serviceProvider.GetService<IHubActivator<LongRunningHub>>() as CustomHubActivator<LongRunningHub>;

                await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).DefaultTimeout();

                // Completion message for canceled Stream
                completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(streamInvocationId, completion.InvocationId);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                await client.Connection.Application.Output.WriteAsync(Encoding.UTF8.GetBytes(new[] { '{' })).DefaultTimeout();

                // Close connection
                client.Connection.Application.Output.Complete();

                // Ignore message from OnConnectedAsync
                await client.ReadAsync().DefaultTimeout();

                var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());

                Assert.Equal("Connection closed with an error. InvalidDataException: Connection terminated while reading a message.", closeMessage.Error);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("invocationId", nameof(MethodHub.StreamDontRead), new[] { "id" }, Array.Empty<object>()).DefaultTimeout();

            foreach (var letter in new[] { "A", "B", "C", "D", "E" })
            {
                await client.SendHubMessageAsync(new StreamItemMessage("id", letter)).DefaultTimeout();
            }

            var ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await client.SendInvocationAsync("Echo", "test");
                var result = (CompletionMessage)await client.ReadAsync().DefaultTimeout(5000);
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
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), new[] { "id" }, Array.Empty<object>());

            foreach (var letter in new[] { "B", "E", "A", "N", "E", "D" })
            {
                await client.SendHubMessageAsync(new StreamItemMessage("id", letter)).DefaultTimeout();
            }

            await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();
            var result = (CompletionMessage)await client.ReadAsync().DefaultTimeout();

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
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadArray), new[] { "id" }, Array.Empty<object>());

            var objects = new[] { new SampleObject("solo", 322), new SampleObject("ggez", 3145) };
            foreach (var thing in objects)
            {
                await client.SendHubMessageAsync(new StreamItemMessage("id", thing)).DefaultTimeout();
            }

            await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();
            var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
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
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
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
                await client.SendHubMessageAsync(new StreamItemMessage(spot.ToString(CultureInfo.InvariantCulture), words[spot][pos[spot]])).DefaultTimeout();
                pos[spot] += 1;
            }

            foreach (string id in new[] { "0", "2", "1" })
            {
                await client.SendHubMessageAsync(CompletionMessage.Empty(id)).DefaultTimeout();
                var response = await client.ReadAsync().DefaultTimeout();
                Debug.Write(response);
                Assert.Equal(words[int.Parse(id, CultureInfo.InvariantCulture)], ((CompletionMessage)response).Result);
            }
        }
    }

    private class DelayRequirement : AuthorizationHandler<DelayRequirement, HubInvocationContext>, IAuthorizationRequirement
    {
        private readonly TcsService _tcsService;
        public DelayRequirement(TcsService tcsService)
        {
            _tcsService = tcsService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DelayRequirement requirement, HubInvocationContext resource)
        {
            _tcsService.StartedMethod.SetResult(null);
            await _tcsService.EndMethod.Task;
            context.Succeed(requirement);
        }
    }

    [Fact]
    // Test to check if StreamItems can be processed before the Stream from the invocation is properly registered internally
    public async Task UploadStreamStreamItemsSentAsSoonAsPossible()
    {
        // Use Auth as the delay injection point because it is one of the first things to run after the invocation message has been parsed
        var tcsService = new TcsService();
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("test", policy =>
                {
                    policy.Requirements.Add(new DelayRequirement(tcsService));
                });
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadArrayAuth), new[] { "id" }, Array.Empty<object>());
            await tcsService.StartedMethod.Task.DefaultTimeout();

            var objects = new[] { new SampleObject("solo", 322), new SampleObject("ggez", 3145) };
            foreach (var thing in objects)
            {
                await client.SendHubMessageAsync(new StreamItemMessage("id", thing)).DefaultTimeout();
            }

            tcsService.EndMethod.SetResult(null);

            await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();
            var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
            var result = ((JArray)response.Result).ToArray<object>();

            Assert.Equal(objects[0].Foo, ((JContainer)result[0])["foo"]);
            Assert.Equal(objects[0].Bar, ((JContainer)result[0])["bar"]);
            Assert.Equal(objects[1].Foo, ((JContainer)result[1])["foo"]);
            Assert.Equal(objects[1].Bar, ((JContainer)result[1])["bar"]);
        }
    }

    [Fact]
    public async Task UploadStreamDoesNotCountTowardsMaxInvocationLimit()
    {
        var tcsService = new TcsService();
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
        {
            services.AddSignalR(options => options.MaximumParallelInvocationsPerClient = 1);
            services.AddSingleton(tcsService);
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("invocation", nameof(LongRunningHub.Upload), new[] { "id" }, Array.Empty<object>());
            await tcsService.StartedMethod.Task.DefaultTimeout();

            var completion = await client.InvokeAsync(nameof(LongRunningHub.SimpleMethod)).DefaultTimeout();
            Assert.Null(completion.Error);
            Assert.Equal(21L, completion.Result);

            await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();

            await tcsService.EndMethod.Task.DefaultTimeout();
            var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
            Assert.Null(response.Result);
            Assert.Null(response.Error);
        }
    }

    [Fact]
    public async Task ConnectionAbortedIfSendFailsWithProtocolError()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedWritingMessage"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options => options.EnableDetailedErrors = true);
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                await client.SendInvocationAsync(nameof(MethodHub.ProtocolError)).DefaultTimeout();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task SerializationExceptionsSendSelfArePassedToOnDisconnectedAsync()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedWritingMessage"))
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Test HubConnectionContext.WriteCore(HubMessage) codepath
                await client.SendInvocationAsync(nameof(ConnectionLifetimeHub.ProtocolErrorSelf)).DefaultTimeout();

                await connectionHandlerTask.DefaultTimeout();

                Assert.IsType<System.Text.Json.JsonException>(state.DisconnectedException);
            }
        }
    }

    [Fact]
    public async Task SerializationExceptionsSendAllArePassedToOnDisconnectedAsync()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedWritingMessage"))
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Test HubConnectionContext.WriteCore(SerializedHubMessage) codepath
                await client.SendInvocationAsync(nameof(ConnectionLifetimeHub.ProtocolErrorAll)).DefaultTimeout();

                await connectionHandlerTask.DefaultTimeout();

                Assert.IsType<System.Text.Json.JsonException>(state.DisconnectedException);
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, Array.Empty<object>()).DefaultTimeout();

                // send integers that are then cast to strings
                await client.SendHubMessageAsync(new StreamItemMessage("id", 5)).DefaultTimeout();
                await client.SendHubMessageAsync(new StreamItemMessage("id", 10)).DefaultTimeout();

                await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();
                var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();

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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                Assert.NotNull(client.HandshakeResponseMessage);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.BeginUploadStreamAsync("invocationId", nameof(MethodHub.TestTypeCastingErrors), new[] { "channelId" }, Array.Empty<object>()).DefaultTimeout();

                // client is running wild, sending strings not ints.
                // this error should be propogated to the user's HubMethod code
                await client.SendHubMessageAsync(new StreamItemMessage("channelId", "not a number")).DefaultTimeout();
                var response = await client.ReadAsync().DefaultTimeout();

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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.SendHubMessageAsync(new StreamItemMessage("fake_id", "not a number")).DefaultTimeout();

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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.SendHubMessageAsync(CompletionMessage.Empty("fake_id")).DefaultTimeout();

                var message = client.TryRead();
                Assert.Null(message);
            }
        }

        Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
            w.EventId.Name == "UnexpectedCompletion"));
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
                await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.TestCustomErrorPassing), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();
                await client.SendHubMessageAsync(CompletionMessage.WithError("id", CustomErrorMessage)).DefaultTimeout();

                var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
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
                await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id", "id2" }, args: Array.Empty<object>()).DefaultTimeout();

                var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
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
                await client.ConnectAsync(connectionHandler).DefaultTimeout();
                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: Array.Empty<string>(), args: Array.Empty<object>()).DefaultTimeout();

                var response = (CompletionMessage)await client.ReadAsync().DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();

                await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).DefaultTimeout();
                await client.SendHubMessageAsync(new StreamItemMessage("id", " world")).DefaultTimeout();
                await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();
                var result = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                Assert.Equal("hello world", simpleCompletion.Result);

                var hubActivator = serviceProvider.GetService<IHubActivator<MethodHub>>() as CustomHubActivator<MethodHub>;

                // OnConnectedAsync and StreamingConcat hubs have been disposed
                Assert.Equal(2, hubActivator.ReleaseCount);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var hubActivator = serviceProvider.GetService<IHubActivator<MethodHub>>() as CustomHubActivator<MethodHub>;
                var createTask = hubActivator.CreateTask.Task;

                // null ID means we're doing a Send and not an Invoke
                await client.BeginUploadStreamAsync(invocationId: null, nameof(MethodHub.StreamingConcat), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();
                await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).DefaultTimeout();
                await client.SendHubMessageAsync(new StreamItemMessage("id", " world")).DefaultTimeout();

                await createTask.DefaultTimeout();
                var tcs = hubActivator.ReleaseTask;
                await client.SendHubMessageAsync(CompletionMessage.Empty("id")).DefaultTimeout();

                await tcs.Task.DefaultTimeout();

                // OnConnectedAsync and StreamingConcat hubs have been disposed
                Assert.Equal(2, hubActivator.ReleaseCount);

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadIgnoreItems), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();

                await client.SendHubMessageAsync(new StreamItemMessage("id", "ignored")).DefaultTimeout();
                var result = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                Assert.Null(simpleCompletion.Result);

                // This will log a warning on the server as the hub method has completed and will complete all associated streams
                await client.SendHubMessageAsync(new StreamItemMessage("id", "error!")).DefaultTimeout();

                // Check that the connection hasn't been closed
                await client.SendInvocationAsync("VoidMethod").DefaultTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                await client.SendStreamInvocationAsync(nameof(MethodHub.StreamAndUploadIgnoreItems), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();

                await client.SendHubMessageAsync(new StreamItemMessage("id", "ignored")).DefaultTimeout();
                var result = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                Assert.Null(simpleCompletion.Result);

                // This will log a warning on the server as the hub method has completed and will complete all associated streams
                await client.SendHubMessageAsync(new StreamItemMessage("id", "error!")).DefaultTimeout();

                // Check that the connection hasn't been closed
                await client.SendInvocationAsync("VoidMethod").DefaultTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var streamInvocationId = await client.SendStreamInvocationAsync(methodName, args).DefaultTimeout();
                // Wait for the stream method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Cancel the stream which should trigger the CancellationToken in the hub method
                await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).DefaultTimeout();

                var result = await client.ReadAsync().DefaultTimeout();

                var simpleCompletion = Assert.IsType<CompletionMessage>(result);
                Assert.Null(simpleCompletion.Result);

                // CancellationToken passed to hub method will allow EndMethod to be triggered if it is canceled.
                await tcsService.EndMethod.Task.DefaultTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Theory]
    [InlineData(nameof(LongRunningHub.CountingCancelableStreamGeneratedAsyncEnumerable), 2)]
    [InlineData(nameof(LongRunningHub.CountingCancelableStreamGeneratedChannel), 2)]
    public async Task CancellationAfterGivenMessagesEndsStreaming(string methodName, int count)
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<LongRunningHub>>();
            var invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(string));

            using (var client = new TestClient(invocationBinder: invocationBinder.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Start streaming count number of messages.
                var invocationId = await client.SendStreamInvocationAsync(methodName, count).DefaultTimeout();

                // Listening on incoming messages
                var listeningMessages = client.ListenAsync(invocationId);

                // Wait for the number of messages expected to be received. This point the sender just waits forever or until cancellation.
                await listeningMessages.ReadAsync(count).DefaultTimeout();

                // Send cancellation.
                await client.SendHubMessageAsync(new CancelInvocationMessage(invocationId)).DefaultTimeout();

                // Wait for the completion message.
                var messages = await listeningMessages.ReadAllAsync().DefaultTimeout();
                Assert.Single(messages);

                // CancellationToken passed to hub method will allow EndMethod to be triggered if it is canceled.
                await tcsService.EndMethod.Task.DefaultTimeout();

                // Shut down
                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.CancelableStreamSingleParameter)).DefaultTimeout();
                // Wait for the stream method to start
                await tcsService.StartedMethod.Task.DefaultTimeout();

                // Shut down the client which should trigger the CancellationToken in the hub method
                client.Dispose();

                // CancellationToken passed to hub method will allow EndMethod to be triggered if it is canceled.
                await tcsService.EndMethod.Task.DefaultTimeout();

                await connectionHandlerTask.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.StreamNullableParameter), 5, null).DefaultTimeout();
                // Wait for the stream method to start
                var firstArgument = await tcsService.StartedMethod.Task.DefaultTimeout();
                Assert.Equal(5, firstArgument);

                var secondArgument = await tcsService.EndMethod.Task.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var streamInvocationId = await client.SendStreamInvocationAsync(nameof(LongRunningHub.CancelableStreamNullableParameter), 5, null).DefaultTimeout();
                // Wait for the stream method to start
                var firstArgument = await tcsService.StartedMethod.Task.DefaultTimeout();
                Assert.Equal(5, firstArgument);

                // Cancel the stream which should trigger the CancellationToken in the hub method
                await client.SendHubMessageAsync(new CancelInvocationMessage(streamInvocationId)).DefaultTimeout();

                var secondArgument = await tcsService.EndMethod.Task.DefaultTimeout();
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
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invocationId = await client.SendInvocationAsync(nameof(MethodHub.InvalidArgument)).DefaultTimeout();

                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());

                Assert.Equal("Failed to invoke 'InvalidArgument' due to an error on the server.", completion.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

                var streamId = "sample_id";
                var messagePromise = client.StreamAsync(nameof(StreamingHub.StreamEcho), new[] { streamId }, Array.Empty<object>()).DefaultTimeout();

                var phrases = new[] { "asdf", "qwer", "zxcv" };
                foreach (var phrase in phrases)
                {
                    await client.SendHubMessageAsync(new StreamItemMessage(streamId, phrase));
                }
                await client.SendHubMessageAsync(CompletionMessage.Empty(streamId));

                var messages = await messagePromise;

                // add one because this includes the completion
                Assert.Equal(phrases.Length + 1, messages.Count);
                for (var i = 0; i < phrases.Length; i++)
                {
                    Assert.Equal("echo:" + phrases[i], ((StreamItemMessage)messages[i]).Item);
                }

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

                var messages = await client.StreamAsync(method, argument).DefaultTimeout();

                Assert.Equal(2, messages.Count);
                HubConnectionHandlerTestUtils.AssertHubMessage(new StreamItemMessage(string.Empty, argument.Value), messages[0]);
                HubConnectionHandlerTestUtils.AssertHubMessage(CompletionMessage.Empty(string.Empty), messages[1]);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
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
                await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

                await callerService.Caller.SendAsync("Echo", "message").DefaultTimeout();

                var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());

                Assert.Equal("Echo", message.Target);
                Assert.Equal("message", message.Arguments[0]);
            }
        }
    }

    [Fact]
    public async Task CanSendThroughIHubContext()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using var client = new TestClient();

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            IHubContext context = (IHubContext)serviceProvider.GetRequiredService<IHubContext<MethodHub>>();
            await context.Clients.All.SendAsync("Send", "test");

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal("test", invocation.Arguments[0]);
            Assert.Equal("Send", invocation.Target);
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
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            await client.BeginUploadStreamAsync("invocation", nameof(MethodHub.UploadDoesWorkOnComplete), streamIds: new[] { "id" }, args: Array.Empty<object>()).DefaultTimeout();

            await client.SendHubMessageAsync(new StreamItemMessage("id", "hello")).DefaultTimeout();

            await client.DisposeAsync().DefaultTimeout();

            await connectionHandlerTask.DefaultTimeout();

            // This task completes if the upload stream is completed, via closing the connection
            var task = (Task<int>)client.Connection.Items[nameof(MethodHub.UploadDoesWorkOnComplete)];

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => task).DefaultTimeout();
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
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            await client.DisposeAsync().DefaultTimeout();

            await connectionHandlerTask.DefaultTimeout();
        }
    }

    [Fact]
    public async Task CanSendThroughIHubContextBaseHub()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using var client = new TestClient();

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            IHubContext<TestHub> context = serviceProvider.GetRequiredService<IHubContext<MethodHub>>();
            await context.Clients.All.SendAsync("Send", "test");

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal("test", invocation.Arguments[0]);
            Assert.Equal("Send", invocation.Target);
        }
    }

    [Fact]
    public async Task HubMethodFailsIfServiceNotFound()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(o => o.EnableDetailedErrors = true);
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.SingleService)).DefaultTimeout();
            Assert.Equal("An unexpected error occurred invoking 'SingleService' on the server. InvalidOperationException: No service for type 'Microsoft.AspNetCore.SignalR.Tests.Service1' has been registered.", res.Error);
        }
    }

    [Fact]
    public async Task HubMethodCanInjectService()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSingleton<Service1>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.SingleService)).DefaultTimeout();
            Assert.True(Assert.IsType<bool>(res.Result));
        }
    }

    [Fact]
    public async Task HubMethodCanInjectMultipleServices()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSingleton<Service1>();
            provider.AddSingleton<Service2>();
            provider.AddSingleton<Service3>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.MultipleServices)).DefaultTimeout();
            Assert.True(Assert.IsType<bool>(res.Result));
        }
    }

    [Fact]
    public async Task HubMethodCanInjectServicesWithOtherParameters()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSingleton<Service1>();
            provider.AddSingleton<Service2>();
            provider.AddSingleton<Service3>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            await client.BeginUploadStreamAsync("0", nameof(ServicesHub.ServicesAndParams), new string[] { "1" }, 10, true).DefaultTimeout();

            await client.SendHubMessageAsync(new StreamItemMessage("1", 1)).DefaultTimeout();
            await client.SendHubMessageAsync(new StreamItemMessage("1", 14)).DefaultTimeout();

            await client.SendHubMessageAsync(CompletionMessage.Empty("1")).DefaultTimeout();

            var response = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Equal(25L, response.Result);
        }
    }

    [Fact]
    public async Task StreamFromServiceDoesNotWork()
    {
        var channel = Channel.CreateBounded<int>(10);
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSingleton(channel.Reader);
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.Stream)).DefaultTimeout();
            Assert.Equal("An unexpected error occurred invoking 'Stream' on the server. HubException: Client sent 0 stream(s), Hub method expects 1.", res.Error);
        }
    }

    [Fact]
    public async Task ServiceNotResolvedWithoutAttribute_WithSettingDisabledGlobally()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.DisableImplicitFromServicesParameters = true;
            });
            provider.AddSingleton<Service1>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal("Failed to invoke 'ServiceWithoutAttribute' due to an error on the server. InvalidDataException: Invocation provides 0 argument(s) but target expects 1.", res.Error);
        }
    }

    [Fact]
    public async Task ServiceResolvedWithoutAttribute()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            provider.AddSingleton<Service1>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal(1L, res.Result);
        }
    }

    [Fact]
    public async Task ServiceResolvedForIEnumerableParameter()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            provider.AddSingleton<Service1>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.IEnumerableOfServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal(1L, res.Result);
        }
    }

    [Fact]
    public async Task ServiceResolvedWithoutAttribute_WithHubSpecificSettingEnabled()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.DisableImplicitFromServicesParameters = true;
            }).AddHubOptions<ServicesHub>(options =>
            {
                options.DisableImplicitFromServicesParameters = false;
            });
            provider.AddSingleton<Service1>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal(1L, res.Result);
        }
    }

    [Fact]
    public async Task ServiceNotResolvedWithAndWithoutAttribute_WithOptionDisabled()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.DisableImplicitFromServicesParameters = true;
            });
            provider.AddSingleton<Service1>();
            provider.AddSingleton<Service2>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithAndWithoutAttribute)).DefaultTimeout();
            Assert.Equal("Failed to invoke 'ServiceWithAndWithoutAttribute' due to an error on the server. InvalidDataException: Invocation provides 0 argument(s) but target expects 1.", res.Error);
        }
    }

    [Fact]
    public async Task ServiceResolvedWithAndWithoutAttribute()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            provider.AddSingleton<Service1>();
            provider.AddSingleton<Service2>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithAndWithoutAttribute)).DefaultTimeout();
            Assert.Equal(1L, res.Result);
        }
    }

    [Fact]
    public async Task ServiceNotResolvedIfNotInDI()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.ServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal("Failed to invoke 'ServiceWithoutAttribute' due to an error on the server. InvalidDataException: Invocation provides 0 argument(s) but target expects 1.", res.Error);
        }
    }

    [Fact]
    public async Task ServiceNotResolvedForIEnumerableParameterIfNotInDI()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(ServicesHub.IEnumerableOfServiceWithoutAttribute)).DefaultTimeout();
            Assert.Equal("Failed to invoke 'IEnumerableOfServiceWithoutAttribute' due to an error on the server. InvalidDataException: Invocation provides 0 argument(s) but target expects 1.", res.Error);
        }
    }

    [Fact]
    public async Task KeyedServiceResolvedIfInDI()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
            provider.AddKeyedScoped<Service1>("service2");
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(KeyedServicesHub.KeyedService)).DefaultTimeout();
            Assert.Equal(43L, res.Result);
        }
    }

    [Fact]
    public async Task HubMethodCanInjectKeyedServiceWithOtherParameters()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
            provider.AddKeyedScoped<Service1>("service2");
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(KeyedServicesHub.KeyedServiceWithParam), 91).DefaultTimeout();
            Assert.Equal(1183L, res.Result);
        }
    }

    [Fact]
    public async Task HubMethodCanInjectKeyedServiceWithNonKeyedService()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
            provider.AddKeyedScoped<Service1>("service2");
            provider.AddScoped<Service2>();
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(KeyedServicesHub.KeyedServiceNonKeyedService)).DefaultTimeout();
            Assert.Equal(11L, res.Result);
        }
    }

    [Fact]
    public async Task MultipleKeyedServicesResolved()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
            provider.AddKeyedScoped<Service1>("service2");
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(KeyedServicesHub.MultipleKeyedServices)).DefaultTimeout();
            Assert.Equal(45L, res.Result);
        }
    }

    [Fact]
    public async Task MultipleKeyedServicesWithSameNameResolved()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
            provider.AddKeyedScoped<Service1>("service2");
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>();

        using (var client = new TestClient())
        {
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
            var res = await client.InvokeAsync(nameof(KeyedServicesHub.MultipleSameKeyedServices)).DefaultTimeout();
            Assert.Equal(445L, res.Result);
        }
    }

    [Fact]
    public void KeyedServiceNotResolvedIfNotInDI()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
        });
        var ex = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<HubConnectionHandler<KeyedServicesHub>>());
        Assert.Equal("'Microsoft.AspNetCore.SignalR.Tests.Service1' is not in DI as a keyed service.", ex.Message);
    }

    [Fact]
    public void KeyedServiceAndFromServiceOnSameParameterInvalidWithKeyedServiceInDI()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            provider.AddKeyedScoped<Service1>("service1");
        });
        var ex = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<HubConnectionHandler<BadServicesHub>>());
        Assert.Equal("BadServicesHub.BadMethod: The FromKeyedServicesAttribute is not supported on parameters that are also annotated with IFromServiceMetadata.", ex.Message);
    }

    [Fact]
    public void TooManyParametersWithServiceThrows()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSingleton<Service1>();
        });
        Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetService<HubConnectionHandler<TooManyParamsHub>>());
    }

    [Fact]
    public async Task SendToAnotherClientFromOnConnectedAsync()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedSendToClientHub>>();

        using (var client1 = new TestClient())
        using (var client2 = new TestClient())
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?client={client1.Connection.ConnectionId}");
            var feature = new TestHttpContextFeature
            {
                HttpContext = httpContext
            };
            client2.Connection.Features.Set<IHttpContextFeature>(feature);

            var connectionHandlerTask = await client1.ConnectAsync(connectionHandler).DefaultTimeout();
            _ = await client2.ConnectAsync(connectionHandler).DefaultTimeout();

            var message = Assert.IsType<InvocationMessage>(await client1.ReadAsync().DefaultTimeout());
            Assert.Single(message.Arguments);
            Assert.Equal(1L, message.Arguments[0]);
            Assert.Equal("Test", message.Target);
        }
    }

    [Fact]
    public async Task ConnectionClosesWhenClientSendsCloseMessage()
    {
        using (StartVerifiableLog())
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using var client = new TestClient();

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            await client.SendHubMessageAsync(new CloseMessage(error: null));

            var message = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Null(message.Error);

            await connectionHandlerTask.DefaultTimeout();

            Assert.Null(state.DisconnectedException);
       }
    }

    [Fact]
    public async Task ConnectionClosesWhenClientSendsCloseMessageWithError()
    {
        using (StartVerifiableLog())
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using var client = new TestClient();

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            var errorMessage = "custom client error";
            await client.SendHubMessageAsync(new CloseMessage(error: errorMessage));

            var message = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
            // Verify no error sent to client
            Assert.Null(message.Error);

            await connectionHandlerTask.DefaultTimeout();

            // Verify OnDisconnectedAsync was called with the error sent by the client
            var ex = Assert.IsType<HubException>(state.DisconnectedException);
            Assert.Equal(errorMessage, ex.Message);
        }
    }

    [Fact]
    public async Task UnsolicitedSequenceAndAckMessagesDoNothing()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

        using (var client = new TestClient())
        {

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

            await client.SendHubMessageAsync(new SequenceMessage(10)).DefaultTimeout();
            await client.SendHubMessageAsync(new AckMessage(234)).DefaultTimeout();

            // Server ignores the above messages, otherwise it would have closed the connection because the values in SequenceMessage and AckMessage aren't valid in this state
            var completionMessage = await client.InvokeAsync(nameof(MethodHub.Echo), new object[] { "test" });

            Assert.Equal("test", completionMessage.Result);
        }
    }

    [Fact]
    public async Task CanSetMessageBufferSizeOnServer()
    {
        var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(provider =>
        {
            provider.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.StatefulReconnectBufferSize = 500;
            });
        });
        var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

        using (var client = new TestClient())
        {
#pragma warning disable CA2252 // This API requires opting into preview features
            client.Connection.Features.Set<IStatefulReconnectFeature>(new EmptyReconnectFeature());
#pragma warning restore CA2252 // This API requires opting into preview features
            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

            await client.InvokeAsync(nameof(MethodHub.Echo), new object[] { new string('x', 500) }).DefaultTimeout();

            // Previous message filled buffer, this message will not send to client until buffer is reduced via Ack
            await client.SendInvocationAsync(nameof(MethodHub.Echo), new object[] { "t" }).DefaultTimeout();

            var readTask = client.ReadAsync();
            Assert.False(readTask.IsCompleted);

            // Remove large message from buffer
            await client.SendHubMessageAsync(new AckMessage(1)).DefaultTimeout();

            var completionMessage = Assert.IsType<CompletionMessage>(await readTask);

            Assert.Equal("t", completionMessage.Result);
        }
    }

#pragma warning disable CA2252 // This API requires opting into preview features
    private class EmptyReconnectFeature : IStatefulReconnectFeature
    {
        public void OnReconnected(Func<PipeWriter, Task> notifyOnReconnect) { }

        public void DisableReconnect()
        {
            throw new NotImplementedException();
        }
    }
#pragma warning restore CA2252 // This API requires opting into preview features

    [Fact]
    public async Task IReconnectNotifyTriggersSequenceMessage()
    {
        using (StartVerifiableLog())
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using var client = new TestClient();
            var reconnectFeature = new TestReconnectFeature();
#pragma warning disable CA2252 // This API requires opting into preview features
            client.Connection.Features.Set<IStatefulReconnectFeature>(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);
            UpdateConnectionPair(client.Connection);

            await reconnectFeature.NotifyOnReconnect(client.Connection.Transport.Output);

            var seqMessage = Assert.IsType<SequenceMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Equal(1, seqMessage.SequenceId);

            await client.SendHubMessageAsync(new SequenceMessage(1)).DefaultTimeout();

            await client.SendHubMessageAsync(new CloseMessage(error: null));

            await connectionHandlerTask.DefaultTimeout();

            Assert.Null(state.DisconnectedException);
        }
    }

    public struct DuplexPipePair
    {
        public IDuplexPipe Transport { get; set; }
        public IDuplexPipe Application { get; set; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }

    private static void UpdateConnectionPair(DefaultConnectionContext connection)
    {
        var prevPipe = connection.Application.Input;
        var input = new Pipe();

        // Add new pipe for reading from and writing to transport from app code
        var transportToApplication = new DuplexPipe(connection.Transport.Input, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, connection.Application.Output);

        connection.Application = applicationToTransport;
        connection.Transport = transportToApplication;

        connection.Transport = connection.Transport;

        // Close previous pipe with specific error that application code can catch to know a restart is occurring
        prevPipe.Complete(new ConnectionResetException(""));
    }

    [Fact]
    public async Task GracefulCloseDisablesReconnect()
    {
        using (StartVerifiableLog())
        {
            var state = new ConnectionLifetimeState();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(s => s.AddSingleton(state), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<ConnectionLifetimeHub>>();

            using var client = new TestClient();

            var reconnectFeature = new TestReconnectFeature();
#pragma warning disable CA2252 // This API requires opting into preview features
            client.Connection.Features.Set<IStatefulReconnectFeature>(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            await client.SendHubMessageAsync(new CloseMessage(error: null));

            await reconnectFeature.ReconnectDisabled.DefaultTimeout();

            var message = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Null(message.Error);

            await connectionHandlerTask.DefaultTimeout();

            Assert.Null(state.DisconnectedException);
        }
    }

#pragma warning disable CA2252 // This API requires opting into preview features
    private class TestReconnectFeature : IStatefulReconnectFeature
#pragma warning restore CA2252 // This API requires opting into preview features
    {
        private TaskCompletionSource _reconnectDisabled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private Func<PipeWriter, Task> _notifyOnReconnect;

        public Task ReconnectDisabled => _reconnectDisabled.Task;

        public Task NotifyOnReconnect(PipeWriter writer)
        {
            return _notifyOnReconnect(writer);
        }

#pragma warning disable CA2252 // This API requires opting into preview features
        public void OnReconnected(Func<PipeWriter, Task> notifyOnReconnect)
        {
            _notifyOnReconnect = notifyOnReconnect;
        }
#pragma warning restore CA2252 // This API requires opting into preview features

#pragma warning disable CA2252 // This API requires opting into preview features
        public void DisableReconnect()
#pragma warning restore CA2252 // This API requires opting into preview features
        {
            _reconnectDisabled.TrySetResult();
        }
    }

    private class CustomHubActivator<THub> : IHubActivator<THub> where THub : Hub
    {
        public int ReleaseCount;
        private readonly IServiceProvider _serviceProvider;
        public TaskCompletionSource ReleaseTask = new TaskCompletionSource();
        public TaskCompletionSource CreateTask = new TaskCompletionSource();

        public CustomHubActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public THub Create()
        {
            ReleaseTask = new TaskCompletionSource();
            var hub = new DefaultHubActivator<THub>(_serviceProvider).Create();
            CreateTask.TrySetResult();
            return hub;
        }

        public void Release(THub hub)
        {
            ReleaseCount++;
            hub.Dispose();
            ReleaseTask.TrySetResult();
            CreateTask = new TaskCompletionSource();
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

public static class IAsyncEnumerableExtension
{
    public static async Task<IEnumerable<T>> ReadAsync<T>(this IAsyncEnumerable<T> enumerable, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Input must be greater than zero.", nameof(count));
        }

        var result = new List<T>();
        await foreach (var item in enumerable)
        {
            result.Add(item);
            if (result.Count == count)
            {
                break;
            }
        }
        return result;
    }

    public static async Task<IEnumerable<T>> ReadAllAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var result = new List<T>();
        await foreach (var item in enumerable)
        {
            result.Add(item);
        }

        return result;
    }
}

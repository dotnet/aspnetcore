// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        [Fact]
        public async Task ReconnectIsNotEnabledByDefault()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ShutdownWithError" ||
                        writeContext.EventId.Name == "ServerDisconnectedWithError");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var exception = new Exception();

                var testConnection = new TestConnection();
                await using var hubConnection = CreateHubConnection(testConnection, loggerFactory: LoggerFactory);

                var reconnectingCalled = false;
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCalled = true;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().OrTimeout();

                testConnection.CompleteFromTransport(exception);

                Assert.Same(exception, await closedErrorTcs.Task.OrTimeout());
                Assert.False(reconnectingCalled);
            }
        }

        [Fact]
        public async Task ReconnectCanBeOptedInto()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError");
            }

            var failReconnectTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = default(ReconnectingConnectionFactory);
                var startCallCount = 0;
                var originalConnectionId = "originalConnectionId";
                var reconnectedConnectionId = "reconnectedConnectionId";

                Task OnTestConnectionStart()
                {
                    startCallCount++;

                    // Only fail the first reconnect attempt.
                    if (startCallCount == 2)
                    {
                        return failReconnectTcs.Task;
                    }

                    // Change the connection id before reconnecting.
                    if (startCallCount == 3)
                    {
                        testConnectionFactory.CurrentTestConnection.ConnectionId = reconnectedConnectionId;
                    }
                    else
                    {
                        testConnectionFactory.CurrentTestConnection.ConnectionId = originalConnectionId;
                    }

                    return Task.CompletedTask;
                }

                testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(OnTestConnectionStart));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);
                    return TimeSpan.Zero;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var reconnectedConnectionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    reconnectedConnectionIdTcs.SetResult(connectionId);
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().OrTimeout();

                Assert.Same(originalConnectionId, hubConnection.ConnectionId);

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                var reconnectException = new Exception();
                failReconnectTcs.SetException(reconnectException);

                Assert.Same(reconnectedConnectionId, await reconnectedConnectionIdTcs.Task.OrTimeout());

                Assert.Equal(2, retryContexts.Count);
                Assert.Same(reconnectException, retryContexts[1].RetryReason);
                Assert.Equal(1, retryContexts[1].PreviousRetryCount);
                Assert.True(TimeSpan.Zero <= retryContexts[1].ElapsedTime);

                await hubConnection.StopAsync().OrTimeout();

                var closeError = await closedErrorTcs.Task.OrTimeout();
                Assert.Null(closeError);
                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectStopsIfTheReconnectPolicyReturnsNull()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError");
            }

            var failReconnectTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var startCallCount = 0;

                Task OnTestConnectionStart()
                {
                    startCallCount++;

                    // Fail the first reconnect attempts.
                    if (startCallCount > 1)
                    {
                        return failReconnectTcs.Task;
                    }

                    return Task.CompletedTask;
                }

                var testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(OnTestConnectionStart));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);
                    return context.PreviousRetryCount == 0 ? TimeSpan.Zero : (TimeSpan?)null;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                var reconnectException = new Exception();
                failReconnectTcs.SetException(reconnectException);

                var closeError = await closedErrorTcs.Task.OrTimeout();
                Assert.IsType<OperationCanceledException>(closeError);

                Assert.Equal(2, retryContexts.Count);
                Assert.Same(reconnectException, retryContexts[1].RetryReason);
                Assert.Equal(1, retryContexts[1].PreviousRetryCount);
                Assert.True(TimeSpan.Zero <= retryContexts[1].ElapsedTime);

                Assert.Equal(1, reconnectingCount);
                Assert.Equal(0, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectCanHappenMultipleTimes()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory();
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);
                    return TimeSpan.Zero;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var reconnectedConnectionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    reconnectedConnectionIdTcs.SetResult(connectionId);
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                await reconnectedConnectionIdTcs.Task.OrTimeout();

                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
                Assert.Equal(TaskStatus.WaitingForActivation, closedErrorTcs.Task.Status);

                reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                reconnectedConnectionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

                var secondException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(secondException);

                Assert.Same(secondException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Equal(2, retryContexts.Count);
                Assert.Same(secondException, retryContexts[1].RetryReason);
                Assert.Equal(0, retryContexts[1].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[1].ElapsedTime);

                await reconnectedConnectionIdTcs.Task.OrTimeout();

                Assert.Equal(2, reconnectingCount);
                Assert.Equal(2, reconnectedCount);
                Assert.Equal(TaskStatus.WaitingForActivation, closedErrorTcs.Task.Status);

                await hubConnection.StopAsync().OrTimeout();

                var closeError = await closedErrorTcs.Task.OrTimeout();
                Assert.Null(closeError);
                Assert.Equal(2, reconnectingCount);
                Assert.Equal(2, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectEventsNotFiredIfFirstRetryDelayIsNull()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       writeContext.EventId.Name == "ServerDisconnectedWithError";
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory();
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<TimeSpan?>(null);
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                await closedErrorTcs.Task.OrTimeout();

                Assert.Equal(0, reconnectingCount);
                Assert.Equal(0, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectDoesntStartIfConnectionIsLostDuringInitialHandshake()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ErrorReceivingHandshakeResponse" ||
                        writeContext.EventId.Name == "ErrorStartingConnection");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(autoHandshake: false));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<TimeSpan?>(null);
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var closedCount = 0;

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedCount++;
                    return Task.CompletedTask;
                };

                var startTask = hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await Assert.ThrowsAsync<Exception>(() => startTask).OrTimeout());
                Assert.Equal(HubConnectionState.Disconnected, hubConnection.State);
                Assert.Equal(0, reconnectingCount);
                Assert.Equal(0, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectContinuesIfConnectionLostDuringReconnectHandshake()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError" ||
                        writeContext.EventId.Name == "ErrorReceivingHandshakeResponse" ||
                        writeContext.EventId.Name == "ErrorStartingConnection");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(autoHandshake: false));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var secondRetryDelayTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);

                    if (retryContexts.Count == 2)
                    {
                        secondRetryDelayTcs.SetResult(null);
                    }

                    return TimeSpan.Zero;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var reconnectedConnectionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    reconnectedConnectionIdTcs.SetResult(connectionId);
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                var startTask = hubConnection.StartAsync();

                // Complete handshake
                await testConnectionFactory.CurrentTestConnection.ReadHandshakeAndSendResponseAsync().OrTimeout();

                await startTask.OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                var secondException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(secondException);

                await secondRetryDelayTcs.Task.OrTimeout();

                Assert.Equal(2, retryContexts.Count);
                Assert.Same(secondException, retryContexts[1].RetryReason);
                Assert.Equal(1, retryContexts[1].PreviousRetryCount);
                Assert.True(TimeSpan.Zero <= retryContexts[0].ElapsedTime);

                // Complete handshake
                await testConnectionFactory.CurrentTestConnection.ReadHandshakeAndSendResponseAsync().OrTimeout();
                await reconnectedConnectionIdTcs.Task.OrTimeout();

                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
                Assert.Equal(TaskStatus.WaitingForActivation, closedErrorTcs.Task.Status);

                await hubConnection.StopAsync().OrTimeout();

                var closeError = await closedErrorTcs.Task.OrTimeout();
                Assert.Null(closeError);
                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectContinuesIfInvalidHandshakeHandshakeResponse()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError" ||
                        writeContext.EventId.Name == "ErrorReceivingHandshakeResponse" ||
                        writeContext.EventId.Name == "HandshakeServerError" ||
                        writeContext.EventId.Name == "ErrorStartingConnection");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(autoHandshake: false));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var secondRetryDelayTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);

                    if (retryContexts.Count == 2)
                    {
                        secondRetryDelayTcs.SetResult(null);
                    }

                    return TimeSpan.Zero;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var reconnectedConnectionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    reconnectedConnectionIdTcs.SetResult(connectionId);
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                var startTask = hubConnection.StartAsync();

                // Complete handshake
                await testConnectionFactory.CurrentTestConnection.ReadHandshakeAndSendResponseAsync().OrTimeout();

                await startTask.OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                // Respond to handshake with error.
                await testConnectionFactory.CurrentTestConnection.ReadSentTextMessageAsync().OrTimeout();

                var output = MemoryBufferWriter.Get();
                try
                {
                    HandshakeProtocol.WriteResponseMessage(new HandshakeResponseMessage("Error!"), output);
                    await testConnectionFactory.CurrentTestConnection.Application.Output.WriteAsync(output.ToArray()).OrTimeout();
                }
                finally
                {
                    MemoryBufferWriter.Return(output);
                }

                await secondRetryDelayTcs.Task.OrTimeout();

                Assert.Equal(2, retryContexts.Count);
                Assert.IsType<HubException>(retryContexts[1].RetryReason);
                Assert.Equal(1, retryContexts[1].PreviousRetryCount);
                Assert.True(TimeSpan.Zero <= retryContexts[0].ElapsedTime);

                // Complete handshake
                await testConnectionFactory.CurrentTestConnection.ReadHandshakeAndSendResponseAsync().OrTimeout();
                await reconnectedConnectionIdTcs.Task.OrTimeout();

                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
                Assert.Equal(TaskStatus.WaitingForActivation, closedErrorTcs.Task.Status);

                await hubConnection.StopAsync().OrTimeout();

                var closeError = await closedErrorTcs.Task.OrTimeout();
                Assert.Null(closeError);
                Assert.Equal(1, reconnectingCount);
                Assert.Equal(1, reconnectedCount);
            }
        }

        [Fact]
        public async Task ReconnectCanBeStoppedWhileRestartingUnderlyingConnection()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError" ||
                        writeContext.EventId.Name == "ErrorReceivingHandshakeResponse" ||
                        writeContext.EventId.Name == "ErrorStartingConnection");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var connectionStartTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                async Task OnTestConnectionStart()
                {
                    try
                    {
                        await connectionStartTcs.Task;
                    }
                    finally
                    {
                        connectionStartTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);;;;
                    }
                }

                var testConnectionFactory = new ReconnectingConnectionFactory(() => new TestConnection(OnTestConnectionStart));
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);
                    return TimeSpan.Zero;
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                // Allow the first connection to start successfully.
                connectionStartTcs.SetResult(null);
                await hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                var secondException = new Exception();
                var stopTask = hubConnection.StopAsync();
                connectionStartTcs.SetResult(null);

                Assert.IsType<OperationCanceledException>(await closedErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Equal(1, reconnectingCount);
                Assert.Equal(0, reconnectedCount);
                Assert.Equal(TaskStatus.RanToCompletion, stopTask.Status);
            }
        }

        [Fact]
        public async Task ReconnectCanBeStoppedDuringRetryDelay()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       (writeContext.EventId.Name == "ServerDisconnectedWithError" ||
                        writeContext.EventId.Name == "ReconnectingWithError" ||
                        writeContext.EventId.Name == "ErrorReceivingHandshakeResponse" ||
                        writeContext.EventId.Name == "ErrorStartingConnection");
            }

            using (StartVerifiableLog(ExpectedErrors))
            {
                var builder = new HubConnectionBuilder().WithLoggerFactory(LoggerFactory);
                var testConnectionFactory = new ReconnectingConnectionFactory();
                builder.Services.AddSingleton<IConnectionFactory>(testConnectionFactory);

                var retryContexts = new List<RetryContext>();
                var mockReconnectPolicy = new Mock<IRetryPolicy>();
                mockReconnectPolicy.Setup(p => p.NextRetryDelay(It.IsAny<RetryContext>())).Returns<RetryContext>(context =>
                {
                    retryContexts.Add(context);
                    // Hopefully this test never takes over a minute.
                    return TimeSpan.FromMinutes(1);
                });
                builder.WithAutomaticReconnect(mockReconnectPolicy.Object);

                await using var hubConnection = builder.Build();
                var reconnectingCount = 0;
                var reconnectedCount = 0;
                var reconnectingErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
                var closedErrorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += error =>
                {
                    reconnectingCount++;
                    reconnectingErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedCount++;
                    return Task.CompletedTask;
                };

                hubConnection.Closed += error =>
                {
                    closedErrorTcs.SetResult(error);
                    return Task.CompletedTask;
                };

                // Allow the first connection to start successfully.
                await hubConnection.StartAsync().OrTimeout();

                var firstException = new Exception();
                testConnectionFactory.CurrentTestConnection.CompleteFromTransport(firstException);

                Assert.Same(firstException, await reconnectingErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Same(firstException, retryContexts[0].RetryReason);
                Assert.Equal(0, retryContexts[0].PreviousRetryCount);
                Assert.Equal(TimeSpan.Zero, retryContexts[0].ElapsedTime);

                await hubConnection.StopAsync();

                Assert.IsType<OperationCanceledException>(await closedErrorTcs.Task.OrTimeout());
                Assert.Single(retryContexts);
                Assert.Equal(1, reconnectingCount);
                Assert.Equal(0, reconnectedCount);
            }
        }

        private class ReconnectingConnectionFactory : IConnectionFactory
        {
            public readonly Func<TestConnection> _testConnectionFactory;
            public TestConnection CurrentTestConnection { get; private set; }

            public ReconnectingConnectionFactory()
                : this (() => new TestConnection())
            {
            }

            public ReconnectingConnectionFactory(Func<TestConnection> testConnectionFactory)
            {
                _testConnectionFactory = testConnectionFactory;
            }

            public Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default)
            {
                CurrentTestConnection = _testConnectionFactory();
                return CurrentTestConnection.StartAsync(transferFormat);
            }

            public Task DisposeAsync(ConnectionContext connection)
            {
                var disposingTestConnection = CurrentTestConnection;
                CurrentTestConnection = null;

                return disposingTestConnection.DisposeAsync();
            }
        }
    }
}

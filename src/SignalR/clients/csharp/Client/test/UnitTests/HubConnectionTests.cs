// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests : VerifiableLoggedTest
    {
        [Fact]
        public async Task InvokeThrowsIfSerializingMessageFails()
        {
            var exception = new InvalidOperationException();
            var hubConnection = CreateHubConnection(new TestConnection(), protocol: MockHubProtocol.Throw(exception));
            await hubConnection.StartAsync().OrTimeout();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.InvokeAsync<int>("test").OrTimeout());
            Assert.Same(exception, actualException);
        }

        [Fact]
        public async Task SendAsyncThrowsIfSerializingMessageFails()
        {
            var exception = new InvalidOperationException();
            var hubConnection = CreateHubConnection(new TestConnection(), protocol: MockHubProtocol.Throw(exception));
            await hubConnection.StartAsync().OrTimeout();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.SendAsync("test").OrTimeout());
            Assert.Same(exception, actualException);
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsStopped()
        {
            var builder = new HubConnectionBuilder();

            var delegateConnectionFactory = new DelegateConnectionFactory(
                format => new TestConnection().StartAsync(format),
                connection => ((TestConnection)connection).DisposeAsync());
            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            var hubConnection = builder.Build();
            var closedEventTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += e =>
            {
                closedEventTcs.SetResult(e);
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync().OrTimeout();
            await hubConnection.StopAsync().OrTimeout();
            Assert.Null(await closedEventTcs.Task);
        }

        [Fact]
        public async Task PendingInvocationsAreCanceledWhenConnectionClosesCleanly()
        {
            var hubConnection = CreateHubConnection(new TestConnection());

            await hubConnection.StartAsync().OrTimeout();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod").OrTimeout();
            await hubConnection.StopAsync().OrTimeout();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }

        [Fact]
        public async Task PendingInvocationsAreTerminatedWithExceptionWhenTransportCompletesWithError()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, protocol: Mock.Of<IHubProtocol>());

            await hubConnection.StartAsync().OrTimeout();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod").OrTimeout();

            var exception = new InvalidOperationException();
            connection.CompleteFromTransport(exception);

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await invokeTask);
            Assert.Equal(exception, actualException);
        }

        [Fact]
        public async Task ConnectionTerminatedIfServerTimeoutIntervalElapsesWithNoMessages()
        {
            var hubConnection = CreateHubConnection(new TestConnection());
            hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(100);

            var closeTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += ex =>
            {
                closeTcs.TrySetResult(ex);
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync().OrTimeout();

            var exception = Assert.IsType<TimeoutException>(await closeTcs.Task.OrTimeout());

            // We use an interpolated string so the tests are accurate on non-US machines.
            Assert.Equal($"Server timeout ({hubConnection.ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.", exception.Message);
        }

        [Fact]
        public async Task ServerTimeoutIsDisabledWhenUsingTransportWithInherentKeepAlive()
        {
            using (StartVerifiableLog())
            {
                var testConnection = new TestConnection();
                testConnection.Features.Set<IConnectionInherentKeepAliveFeature>(new TestKeepAliveFeature() { HasInherentKeepAlive = true });
                var hubConnection = CreateHubConnection(testConnection, loggerFactory: LoggerFactory);
                hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(1);

                await hubConnection.StartAsync().OrTimeout();

                var closeTcs = new TaskCompletionSource<Exception>();
                hubConnection.Closed += ex =>
                {
                    closeTcs.TrySetResult(ex);
                    return Task.CompletedTask;
                };

                await hubConnection.RunTimerActions().OrTimeout();

                Assert.False(closeTcs.Task.IsCompleted);

                await hubConnection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        [LogLevel(LogLevel.Trace)]
        public async Task PendingInvocationsAreTerminatedIfServerTimeoutIntervalElapsesWithNoMessages()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == typeof(HubConnection).FullName &&
                       writeContext.EventId.Name == "ShutdownWithError";
            }

            using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
            {
                var hubConnection = CreateHubConnection(new TestConnection(), loggerFactory: LoggerFactory);
                hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(2000);

                await hubConnection.StartAsync().OrTimeout();

                // Start an invocation (but we won't complete it)
                var invokeTask = hubConnection.InvokeAsync("Method").OrTimeout();

                var exception = await Assert.ThrowsAsync<TimeoutException>(() => invokeTask);

                // We use an interpolated string so the tests are accurate on non-US machines.
                Assert.Equal($"Server timeout ({hubConnection.ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.", exception.Message);
            }
        }

        private struct TestKeepAliveFeature : IConnectionInherentKeepAliveFeature
        {
            public bool HasInherentKeepAlive { get; set; }
        }

        // Moq really doesn't handle out parameters well, so to make these tests work I added a manual mock -anurse
        private class MockHubProtocol : IHubProtocol
        {
            private HubInvocationMessage _parsed;
            private Exception _error;

            public static MockHubProtocol ReturnOnParse(HubInvocationMessage parsed)
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

            public string Name => "MockHubProtocol";
            public int Version => 1;

            public TransferFormat TransferFormat => TransferFormat.Binary;

            public bool IsVersionSupported(int version)
            {
                return true;
            }

            public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
            {
                if (_error != null)
                {
                    throw _error;
                }
                if (_parsed != null)
                {
                    message = _parsed;
                    return true;
                }

                throw new InvalidOperationException("No Parsed Message provided");
            }

            public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
            {
                if (_error != null)
                {
                    throw _error;
                }
            }

            public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
            {
                return HubProtocolExtensions.GetMessageBytes(this, message);
            }
        }
    }
}

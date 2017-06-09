// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionTests
    {
        [Fact]
        public async Task StartAsyncCallsConnectionStart()
        {
            var connection = new Mock<IConnection>();
            connection.Setup(m => m.StartAsync()).Returns(Task.CompletedTask).Verifiable();
            var hubConnection = new HubConnection(connection.Object);
            await hubConnection.StartAsync();

            connection.Verify(c => c.StartAsync(), Times.Once());
        }

        [Fact]
        public async Task DisposeAsyncCallsConnectionStart()
        {
            var connection = new Mock<IConnection>();
            connection.Setup(m => m.StartAsync()).Verifiable();
            var hubConnection = new HubConnection(connection.Object);
            await hubConnection.DisposeAsync();

            connection.Verify(c => c.DisposeAsync(), Times.Once());
        }

        [Fact]
        public async Task InvokeThrowsIfSerializingMessageFails()
        {
            var exception = new InvalidOperationException();
            var mockProtocol = MockHubProtocol.Throw(exception);
            var hubConnection = new HubConnection(new TestConnection(), mockProtocol, null);
            await hubConnection.StartAsync();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.Invoke<int>("test"));
            Assert.Same(exception, actualException);
        }

        [Fact]
        public async Task HubConnectionConnectedEventRaisedWhenTheClientIsConnected()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection);
            try
            {
                var connectedEventRaisedTcs = new TaskCompletionSource<object>();
                hubConnection.Connected += () => connectedEventRaisedTcs.SetResult(null);

                await hubConnection.StartAsync();

                await connectedEventRaisedTcs.Task.OrTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync();
            }
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsStopped()
        {
            var hubConnection = new HubConnection(new TestConnection());
            var closedEventTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += e => closedEventTcs.SetResult(e);

            await hubConnection.StartAsync();
            await hubConnection.DisposeAsync();

            Assert.Null(await closedEventTcs.Task.OrTimeout());
        }

        [Fact]
        public async Task CannotCallInvokeOnClosedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new LoggerFactory());

            await hubConnection.StartAsync();
            await hubConnection.DisposeAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await hubConnection.Invoke<int>("test"));

            Assert.Equal("Connection has been terminated.", exception.Message);
        }

        [Fact]
        public async Task PendingInvocationsAreCancelledWhenConnectionClosesCleanly()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new LoggerFactory());

            await hubConnection.StartAsync();
            var invokeTask = hubConnection.Invoke<int>("testMethod");
            await hubConnection.DisposeAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }

        [Fact]
        public async Task PendingInvocationsAreTerminatedWithExceptionWhenConnectionClosesDueToError()
        {
            var exception = new InvalidOperationException();
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .Setup(m => m.DisposeAsync())
                .Callback(() => mockConnection.Raise(c => c.Closed += null, exception))
                .Returns(Task.FromResult<object>(null));

            var hubConnection = new HubConnection(mockConnection.Object, new LoggerFactory());

            await hubConnection.StartAsync();
            var invokeTask = hubConnection.Invoke<int>("testMethod");
            await hubConnection.DisposeAsync();

            var thrown = await Assert.ThrowsAsync(exception.GetType(), async () => await invokeTask);
            Assert.Same(exception, thrown);
        }

        [Fact]
        public async Task DoesNotThrowWhenClientMethodCalledButNoInvocationHandlerHasBeenSetUp()
        {
            var mockConnection = new Mock<IConnection>();

            var invocation = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: "NonExistingMethod123", arguments: new object[] { true, "arg2", 123 });

            var mockProtocol = MockHubProtocol.ReturnOnParse(invocation);

            var hubConnection = new HubConnection(mockConnection.Object, mockProtocol, null);
            await hubConnection.StartAsync();

            mockConnection.Raise(c => c.Received += null, new object[] { new byte[] { } });
            Assert.Equal(1, mockProtocol.ParseCalls);
        }

        // Moq really doesn't handle out parameters well, so to make these tests work I added a manual mock -anurse
        private class MockHubProtocol : IHubProtocol
        {
            private HubMessage _parsed;
            private Exception _error;

            public int ParseCalls { get; private set; } = 0;
            public int WriteCalls { get; private set; } = 0;

            public static MockHubProtocol ReturnOnParse(HubMessage parsed)
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

            public bool TryParseMessages(ReadOnlySpan<byte> input, IInvocationBinder binder, out IList<HubMessage> messages)
            {
                messages = new List<HubMessage>();

                ParseCalls += 1;
                if (_error != null)
                {
                    throw _error;
                }
                if (_parsed != null)
                {
                    messages.Add(_parsed);
                    return true;
                }

                throw new InvalidOperationException("No Parsed Message provided");
            }

            public bool TryWriteMessage(HubMessage message, IOutput output)
            {
                WriteCalls += 1;

                if (_error != null)
                {
                    throw _error;
                }
                return true;
            }
        }
    }
}

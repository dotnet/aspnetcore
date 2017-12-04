// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
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
            connection.SetupGet(p => p.Features).Returns(new FeatureCollection());
            connection.Setup(m => m.StartAsync()).Returns(Task.CompletedTask).Verifiable();
            var hubConnection = new HubConnection(connection.Object, Mock.Of<IHubProtocol>(), null);
            await hubConnection.StartAsync();

            connection.Verify(c => c.StartAsync(), Times.Once());
        }

        [Fact]
        public async Task DisposeAsyncCallsConnectionStart()
        {
            var connection = new Mock<IConnection>();
            connection.Setup(m => m.StartAsync()).Verifiable();
            var hubConnection = new HubConnection(connection.Object, Mock.Of<IHubProtocol>(), null);
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
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.InvokeAsync<int>("test"));
            Assert.Same(exception, actualException);
        }

        [Fact]
        public async Task SendAsyncThrowsIfSerializingMessageFails()
        {
            var exception = new InvalidOperationException();
            var mockProtocol = MockHubProtocol.Throw(exception);
            var hubConnection = new HubConnection(new TestConnection(), mockProtocol, null);
            await hubConnection.StartAsync();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.SendAsync("test"));
            Assert.Same(exception, actualException);
        }

        [Fact]
        public async Task ClosedEventRaisedWhenTheClientIsStopped()
        {
            var hubConnection = new HubConnection(new TestConnection(), Mock.Of<IHubProtocol>(), null);
            var closedEventTcs = new TaskCompletionSource<Exception>();

            await hubConnection.StartAsync().OrTimeout();
            await hubConnection.DisposeAsync().OrTimeout();
            await hubConnection.Closed.OrTimeout();
        }

        [Fact]
        public async Task CannotCallInvokeOnNotStartedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => hubConnection.InvokeAsync<int>("test"));

            Assert.Equal("The 'InvokeAsync' method cannot be called before the connection has been started.", exception.Message);
        }

        [Fact]
        public async Task CannotCallInvokeOnClosedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            await hubConnection.StartAsync();
            await hubConnection.DisposeAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => hubConnection.InvokeAsync<int>("test"));

            Assert.Equal("Connection has been terminated.", exception.Message);
        }

        [Fact]
        public async Task CannotCallSendOnNotStartedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => hubConnection.SendAsync("test"));

            Assert.Equal("The 'SendAsync' method cannot be called before the connection has been started.", exception.Message);
        }

        [Fact]
        public async Task CannotCallSendOnClosedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            await hubConnection.StartAsync();
            await hubConnection.DisposeAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => hubConnection.SendAsync("test"));

            Assert.Equal("Connection has been terminated.", exception.Message);
        }

        [Fact]
        public async Task CannotCallStreamOnClosedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            await hubConnection.StartAsync();
            await hubConnection.DisposeAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => hubConnection.StreamAsync<int>("test"));

            Assert.Equal("Connection has been terminated.", exception.Message);
        }

        [Fact]
        public async Task CannotCallStreamOnNotStartedHubConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => hubConnection.StreamAsync<int>("test"));

            Assert.Equal("The 'StreamAsync' method cannot be called before the connection has been started.", exception.Message);
        }

        [Fact]
        public async Task PendingInvocationsAreCancelledWhenConnectionClosesCleanly()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());

            await hubConnection.StartAsync();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod");
            await hubConnection.DisposeAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }

        [Fact]
        public async Task PendingInvocationsAreTerminatedWithExceptionWhenConnectionClosesDueToError()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.SetupGet(p => p.Features).Returns(new FeatureCollection());
            mockConnection
                .Setup(m => m.DisposeAsync())
                .Returns(Task.FromResult<object>(null));

            var hubConnection = new HubConnection(mockConnection.Object, Mock.Of<IHubProtocol>(), new LoggerFactory());

            await hubConnection.StartAsync();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod");
            await hubConnection.DisposeAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await invokeTask);
        }

        // Moq really doesn't handle out parameters well, so to make these tests work I added a manual mock -anurse
        private class MockHubProtocol : IHubProtocol
        {
            private HubInvocationMessage _parsed;
            private Exception _error;

            public int ParseCalls { get; private set; } = 0;
            public int WriteCalls { get; private set; } = 0;

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

            public ProtocolType Type => ProtocolType.Binary;

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

            public void WriteMessage(HubMessage message, Stream output)
            {
                WriteCalls += 1;

                if (_error != null)
                {
                    throw _error;
                }
            }
        }
    }
}

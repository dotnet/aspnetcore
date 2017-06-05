// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    // This includes tests that verify HubConnection conforms to the Hub Protocol, without setting up a full server (even TestServer).
    // We can also have more control over the messages we send to HubConnection in order to ensure that protocol errors and other quirks
    // don't cause problems.
    public class HubConnectionProtocolTests
    {
        [Fact]
        public async Task InvokeSendsAnInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke("Foo");

                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("59:T:{\"invocationId\":\"1\",\"type\":1,\"target\":\"Foo\",\"arguments\":[]};", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task StreamSendsAnInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = hubConnection.Stream<object>("Foo");

                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("59:T:{\"invocationId\":\"1\",\"type\":1,\"target\":\"Foo\",\"arguments\":[]};", invokeMessage);

                // Complete the channel
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();
                await channel.Completion;
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task InvokeCompletedWhenCompletionMessageReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                await invokeTask.OrTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task StreamCompletesWhenCompletionMessageIsReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = hubConnection.Stream<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                Assert.Empty(await channel.ReadAllAsync());
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task InvokeYieldsResultWhenCompletionMessageReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, result = 42 }).OrTimeout();

                Assert.Equal(42, await invokeTask.OrTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task StreamFailsIfCompletionMessageHasPayload()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = hubConnection.Stream<string>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, result = "Oops" }).OrTimeout();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await channel.ReadAllAsync().OrTimeout());
                Assert.Equal("Server provided a result in a completion response to a streamed invocation.", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task InvokeFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, error = "An error occurred" }).OrTimeout();

                var ex = await Assert.ThrowsAsync<HubException>(() => invokeTask).OrTimeout();
                Assert.Equal("An error occurred", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task StreamFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = hubConnection.Stream<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, error = "An error occurred" }).OrTimeout();

                var ex = await Assert.ThrowsAsync<HubException>(async () => await channel.ReadAllAsync().OrTimeout());
                Assert.Equal("An error occurred", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task InvokeFailsWithErrorWhenStreamingItemReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = 42 }).OrTimeout();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => invokeTask).OrTimeout();
                Assert.Equal("Streaming methods must be invoked using HubConnection.Stream", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task StreamYieldsItemsAsTheyArrive()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = hubConnection.Stream<string>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "1" }).OrTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "2" }).OrTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "3" }).OrTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                var notifications = await channel.ReadAllAsync().OrTimeout();

                Assert.Equal(new[] { "1", "2", "3", }, notifications.ToArray());
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task HandlerRegisteredWithOnIsFiredWhenInvocationReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            var handlerCalled = new TaskCompletionSource<object[]>();
            try
            {
                await hubConnection.StartAsync();

                hubConnection.On<int, string, float>("Foo", (r1, r2, r3) => handlerCalled.TrySetResult(new object[] { r1, r2, r3 }));

                var args = new object[] { 1, "Foo", 2.0f };
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = args }).OrTimeout();

                Assert.Equal(args, await handlerCalled.Task.OrTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }
    }
}

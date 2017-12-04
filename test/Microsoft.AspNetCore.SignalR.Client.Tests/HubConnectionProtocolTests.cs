// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
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
        public async Task SendAsyncSendsANonBlockingInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.SendAsync("Foo");

                // skip negotiation
                await connection.ReadSentTextMessageAsync().OrTimeout();
                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("{\"invocationId\":\"1\",\"type\":1,\"target\":\"Foo\",\"nonBlocking\":true,\"arguments\":[]}\u001e", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task ClientSendsNegotationMessageWhenStartingConnection()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();
                var negotiationMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("{\"protocol\":\"json\"}\u001e", negotiationMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task InvokeSendsAnInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.InvokeAsync("Foo");

                // skip negotiation
                await connection.ReadSentTextMessageAsync().OrTimeout();
                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("{\"invocationId\":\"1\",\"type\":1,\"target\":\"Foo\",\"arguments\":[]}\u001e", invokeMessage);
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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = await hubConnection.StreamAsync<object>("Foo");

                // skip negotiation
                await connection.ReadSentTextMessageAsync().OrTimeout();
                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("{\"invocationId\":\"1\",\"type\":4,\"target\":\"Foo\",\"arguments\":[]}\u001e", invokeMessage);

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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.InvokeAsync("Foo");

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

                var channel = await hubConnection.StreamAsync<int>("Foo");

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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

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
        public async Task InvokeFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

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
        public async Task StreamFailsIfCompletionMessageHasPayload()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = await hubConnection.StreamAsync<string>("Foo");

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
        public async Task StreamFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = await hubConnection.StreamAsync<int>("Foo");

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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = 42 }).OrTimeout();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => invokeTask).OrTimeout();
                Assert.Equal("Streaming hub methods must be invoked with the 'HubConnection.StreamAsync' method.", ex.Message);
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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var channel = await hubConnection.StreamAsync<string>("Foo");

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
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(), new LoggerFactory());
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

        [Fact]
        public async Task MessagesEncodedWhenUsingBinaryProtocolOverTextTransport()
        {
            var connection = new TestConnection(TransferMode.Text);

            var hubConnection = new HubConnection(connection,
                new MessagePackHubProtocol(), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync().OrTimeout();
                await hubConnection.SendAsync("MyMethod", 42).OrTimeout();

                await connection.ReadSentTextMessageAsync().OrTimeout();
                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                // The message is in the following format `size:payload;`
                var parts = invokeMessage.Split(':');
                Assert.Equal(2, parts.Length);
                Assert.True(int.TryParse(parts[0], out var payloadSize));
                Assert.Equal(payloadSize, parts[1].Length - 1);
                Assert.EndsWith(";", parts[1]);

                // this throws if the message is not a valid base64 string
                Convert.FromBase64String(parts[1].Substring(0, payloadSize));
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task MessagesDecodedWhenUsingBinaryProtocolOverTextTransport()
        {
            var connection = new TestConnection(TransferMode.Text);
            var hubConnection = new HubConnection(connection,
                new MessagePackHubProtocol(), new LoggerFactory());

            var invocationTcs = new TaskCompletionSource<int>();
            try
            {
                await hubConnection.StartAsync().OrTimeout();
                hubConnection.On<int>("MyMethod", result => invocationTcs.SetResult(result));

                using (var ms = new MemoryStream())
                {
                    new MessagePackHubProtocol()
                        .WriteMessage(new InvocationMessage("1", true, "MyMethod", null, 42), ms);

                    var invokeMessage = Convert.ToBase64String(ms.ToArray());
                    var payloadSize = invokeMessage.Length.ToString(CultureInfo.InvariantCulture);
                    var message = $"{payloadSize}:{invokeMessage};";

                    connection.ReceivedMessages.TryWrite(Encoding.UTF8.GetBytes(message));
                }

                Assert.Equal(42, await invocationTcs.Task.OrTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Fact]
        public async Task AcceptsPingMessages()
        {
            var connection = new TestConnection(TransferMode.Text);
            var hubConnection = new HubConnection(connection,
                new JsonHubProtocol(), new LoggerFactory());

            try
            {
                await hubConnection.StartAsync().OrTimeout();

                // Ignore negotiate message
                await connection.ReadSentTextMessageAsync().OrTimeout();

                // Send an invocation
                var invokeTask = hubConnection.InvokeAsync("Foo");

                // Receive the ping mid-invocation so we can see that the rest of the flow works fine
                await connection.ReceiveJsonMessage(new { type = 6 }).OrTimeout();

                // Receive a completion
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                // Ensure the invokeTask completes properly
                await invokeTask.OrTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }
    }
}

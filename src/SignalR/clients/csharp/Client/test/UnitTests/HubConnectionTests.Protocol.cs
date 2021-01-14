// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    // This includes tests that verify HubConnection conforms to the Hub Protocol, without setting up a full server (even TestServer).
    // We can also have more control over the messages we send to HubConnection in order to ensure that protocol errors and other quirks
    // don't cause problems.
    public partial class HubConnectionTests
    {
        public class Protocol : VerifiableLoggedTest
        {
            [Fact]
            public async Task SendAsyncSendsANonBlockingInvocationMessage()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var invokeTask = hubConnection.SendAsync("Foo").OrTimeout();

                    var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                    // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                    Assert.Equal("{\"type\":1,\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                    await invokeTask.OrTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ClientSendsHandshakeMessageWhenStartingConnection()
            {
                var connection = new TestConnection(autoHandshake: false);
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    // We can't await StartAsync because it depends on the negotiate process!
                    var startTask = hubConnection.StartAsync();

                    var handshakeMessage = await connection.ReadHandshakeAndSendResponseAsync().OrTimeout();

                    // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                    Assert.Equal("{\"protocol\":\"json\",\"version\":1}", handshakeMessage);

                    await startTask.OrTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task InvalidHandshakeResponseCausesStartToFail()
            {
                using (StartVerifiableLog())
                {
                    var connection = new TestConnection(autoHandshake: false);
                    var hubConnection = CreateHubConnection(connection);
                    try
                    {
                        // We can't await StartAsync because it depends on the negotiate process!
                        var startTask = hubConnection.StartAsync();

                        await connection.ReadSentTextMessageAsync().OrTimeout();

                        // The client expects the first message to be a handshake response, but a handshake response doesn't have a "type".
                        await connection.ReceiveJsonMessage(new { type = "foo" }).OrTimeout();

                        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => startTask).OrTimeout();

                        Assert.Equal("Expected a handshake response from the server.", ex.Message);
                    }
                    finally
                    {
                        await hubConnection.DisposeAsync().OrTimeout();
                        await connection.DisposeAsync().OrTimeout();
                    }
                }
            }

            [Fact]
            public async Task ClientIsOkayReceivingMinorVersionInHandshake()
            {
                // We're just testing that the client doesn't fail when a minor version is added to the handshake
                // The client doesn't actually use that version anywhere yet so there's nothing else to test at this time

                var connection = new TestConnection(autoHandshake: false);
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    var startTask = hubConnection.StartAsync();
                    var message = await connection.ReadHandshakeAndSendResponseAsync(56).OrTimeout();

                    await startTask.OrTimeout();
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var invokeTask = hubConnection.InvokeAsync("Foo");

                    var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                    // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                    Assert.Equal("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                    Assert.Equal(TaskStatus.WaitingForActivation, invokeTask.Status);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ReceiveCloseMessageWithoutErrorWillCloseHubConnection()
            {
                var closedTcs = new TaskCompletionSource<Exception>();

                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                hubConnection.Closed += e =>
                {
                    closedTcs.SetResult(e);
                    return Task.CompletedTask;
                };

                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    await connection.ReceiveJsonMessage(new { type = 7 }).OrTimeout();

                    var closeException = await closedTcs.Task.OrTimeout();
                    Assert.Null(closeException);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ReceiveCloseMessageWithErrorWillCloseHubConnection()
            {
                var closedTcs = new TaskCompletionSource<Exception>();

                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                hubConnection.Closed += e =>
                {
                    closedTcs.SetResult(e);
                    return Task.CompletedTask;
                };

                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    await connection.ReceiveJsonMessage(new { type = 7, error = "Error!" }).OrTimeout();

                    var closeException = await closedTcs.Task.OrTimeout();
                    Assert.NotNull(closeException);
                    Assert.Equal("The server closed the connection with the following error: Error!", closeException.Message);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task StreamSendsAnInvocationMessage()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var channel = await hubConnection.StreamAsChannelAsync<object>("Foo").OrTimeout();

                    var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                    // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                    Assert.Equal("{\"type\":4,\"invocationId\":\"1\",\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                    // Complete the channel
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();
                    await channel.Completion.OrTimeout();
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var channel = await hubConnection.StreamAsChannelAsync<int>("Foo").OrTimeout();

                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                    Assert.Empty(await channel.ReadAndCollectAllAsync().OrTimeout());
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var channel = await hubConnection.StreamAsChannelAsync<string>("Foo").OrTimeout();

                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, result = "Oops" }).OrTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.ReadAndCollectAllAsync()).OrTimeout();
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var channel = await hubConnection.StreamAsChannelAsync<int>("Foo").OrTimeout();

                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, error = "An error occurred" }).OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(async () => await channel.ReadAndCollectAllAsync()).OrTimeout();
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var invokeTask = hubConnection.InvokeAsync<int>("Foo");

                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = 42 }).OrTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => invokeTask).OrTimeout();
                    Assert.Equal("Streaming hub methods must be invoked with the 'HubConnection.StreamAsChannelAsync' method.", ex.Message);
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
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var channel = await hubConnection.StreamAsChannelAsync<string>("Foo").OrTimeout();

                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "1" }).OrTimeout();
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "2" }).OrTimeout();
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "3" }).OrTimeout();
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).OrTimeout();

                    var notifications = await channel.ReadAndCollectAllAsync().OrTimeout();

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
                var hubConnection = CreateHubConnection(connection);
                var handlerCalled = new TaskCompletionSource<object[]>();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

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
            public async Task HandlerIsRemovedProperlyWithOff()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                var handlerCalled = new TaskCompletionSource<int>();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    hubConnection.On<int>("Foo", (val) =>
                    {
                        handlerCalled.TrySetResult(val);
                    });

                    hubConnection.Remove("Foo");
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = 1 }).OrTimeout();
                    var handlerTask = handlerCalled.Task;

                    // We expect the handler task to timeout since the handler has been removed with the call to Remove("Foo")
                    var ex = Assert.ThrowsAsync<TimeoutException>(async () => await handlerTask.OrTimeout(2000));

                    // Ensure that the task from the WhenAny is not the handler task
                    Assert.False(handlerCalled.Task.IsCompleted);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task DisposingSubscriptionAfterCallingRemoveHandlerDoesntFail()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                var handlerCalled = new TaskCompletionSource<int>();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var subscription = hubConnection.On<int>("Foo", (val) =>
                    {
                        handlerCalled.TrySetResult(val);
                    });

                    hubConnection.Remove("Foo");
                    await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = 1 }).OrTimeout();
                    var handlerTask = handlerCalled.Task;

                    subscription.Dispose();

                    // We expect the handler task to timeout since the handler has been removed with the call to Remove("Foo")
                    var ex = Assert.ThrowsAsync<TimeoutException>(async () => await handlerTask.OrTimeout(2000));

                    // Ensure that the task from the WhenAny is not the handler task
                    Assert.False(handlerCalled.Task.IsCompleted);
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
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);

                try
                {
                    await hubConnection.StartAsync().OrTimeout();

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

            [Fact]
            public async Task PartialHandshakeResponseWorks()
            {
                var connection = new TestConnection(autoHandshake: false);
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    var task = hubConnection.StartAsync();

                    await connection.ReceiveTextAsync("{").OrTimeout();

                    Assert.False(task.IsCompleted);

                    await connection.ReceiveTextAsync("}").OrTimeout();

                    Assert.False(task.IsCompleted);

                    await connection.ReceiveTextAsync("\u001e").OrTimeout();

                    await task.OrTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task HandshakeAndInvocationInSameBufferWorks()
            {
                var payload = "{}\u001e{\"type\":1, \"target\": \"Echo\", \"arguments\":[\"hello\"]}\u001e";
                var connection = new TestConnection(autoHandshake: false);
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    var tcs = new TaskCompletionSource<string>();
                    hubConnection.On<string>("Echo", data =>
                    {
                        tcs.TrySetResult(data);
                    });

                    await connection.ReceiveTextAsync(payload).OrTimeout();

                    await hubConnection.StartAsync().OrTimeout();

                    var response = await tcs.Task.OrTimeout();
                    Assert.Equal("hello", response);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task PartialInvocationWorks()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                try
                {
                    var tcs = new TaskCompletionSource<string>();
                    hubConnection.On<string>("Echo", data =>
                    {
                        tcs.TrySetResult(data);
                    });

                    await hubConnection.StartAsync().OrTimeout();

                    await connection.ReceiveTextAsync("{\"type\":1, ").OrTimeout();

                    Assert.False(tcs.Task.IsCompleted);

                    await connection.ReceiveTextAsync("\"target\": \"Echo\", \"arguments\"").OrTimeout();

                    Assert.False(tcs.Task.IsCompleted);

                    await connection.ReceiveTextAsync(":[\"hello\"]}\u001e").OrTimeout();

                    var response = await tcs.Task.OrTimeout();

                    Assert.Equal("hello", response);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ClientPingsMultipleTimes()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);

                hubConnection.TickRate = TimeSpan.FromMilliseconds(30);
                hubConnection.KeepAliveInterval = TimeSpan.FromMilliseconds(80);

                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var firstPing = await connection.ReadSentTextMessageAsync(ignorePings: false).OrTimeout();
                    Assert.Equal("{\"type\":6}", firstPing);

                    var secondPing = await connection.ReadSentTextMessageAsync(ignorePings: false).OrTimeout();
                    Assert.Equal("{\"type\":6}", secondPing);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ClientWithInherentKeepAliveDoesNotPing()
            {
                var connection = new TestConnection(hasInherentKeepAlive: true);
                var hubConnection = CreateHubConnection(connection);

                hubConnection.TickRate = TimeSpan.FromMilliseconds(30);
                hubConnection.KeepAliveInterval = TimeSpan.FromMilliseconds(80);

                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    await Task.Delay(1000);

                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();

                    Assert.Equal(0, (await connection.ReadAllSentMessagesAsync(ignorePings: false).OrTimeout()).Count);
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }
    }
}

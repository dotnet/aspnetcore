// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Tests;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

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
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.SendAsync("Foo").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                Assert.Equal("{\"type\":1,\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                await invokeTask.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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

                var handshakeMessage = await connection.ReadHandshakeAndSendResponseAsync().DefaultTimeout();

                // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                Assert.Equal("{\"protocol\":\"json\",\"version\":1}", handshakeMessage);

                await startTask.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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

                    await connection.ReadSentTextMessageAsync().DefaultTimeout();

                    // The client expects the first message to be a handshake response, but a handshake response doesn't have a "type".
                    await connection.ReceiveJsonMessage(new { type = "foo" }).DefaultTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidDataException>(() => startTask).DefaultTimeout();

                    Assert.Equal("Expected a handshake response from the server.", ex.Message);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
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
                var message = await connection.ReadHandshakeAndSendResponseAsync(56).DefaultTimeout();

                await startTask.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeSendsAnInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync("Foo");

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                Assert.Equal("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                Assert.Equal(TaskStatus.WaitingForActivation, invokeTask.Status);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                await connection.ReceiveJsonMessage(new { type = 7 }).DefaultTimeout();

                var closeException = await closedTcs.Task.DefaultTimeout();
                Assert.Null(closeException);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                await connection.ReceiveJsonMessage(new { type = 7, error = "Error!" }).DefaultTimeout();

                var closeException = await closedTcs.Task.DefaultTimeout();
                Assert.NotNull(closeException);
                Assert.Equal("The server closed the connection with the following error: Error!", closeException.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamSendsAnInvocationMessage()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<object>("Foo").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                // ReadSentTextMessageAsync strips off the record separator (because it has use it as a separator now that we use Pipelines)
                Assert.Equal("{\"type\":4,\"invocationId\":\"1\",\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);

                // Complete the channel
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();
                await channel.Completion.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeCompletedWhenCompletionMessageReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();

                await invokeTask.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamCompletesWhenCompletionMessageIsReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<int>("Foo").DefaultTimeout();

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();

                Assert.Empty(await channel.ReadAndCollectAllAsync().DefaultTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeYieldsResultWhenCompletionMessageReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, result = 42 }).DefaultTimeout();

                Assert.Equal(42, await invokeTask.DefaultTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, error = "An error occurred" }).DefaultTimeout();

                var ex = await Assert.ThrowsAsync<HubException>(() => invokeTask).DefaultTimeout();
                Assert.Equal("An error occurred", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamFailsIfCompletionMessageHasPayload()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<string>("Foo").DefaultTimeout();

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, result = "Oops" }).DefaultTimeout();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.ReadAndCollectAllAsync()).DefaultTimeout();
                Assert.Equal("Server provided a result in a completion response to a streamed invocation.", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamFailsWithExceptionWhenCompletionWithErrorReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<int>("Foo").DefaultTimeout();

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3, error = "An error occurred" }).DefaultTimeout();

                var ex = await Assert.ThrowsAsync<HubException>(async () => await channel.ReadAndCollectAllAsync()).DefaultTimeout();
                Assert.Equal("An error occurred", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeFailsWithErrorWhenStreamingItemReceived()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = 42 }).DefaultTimeout();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => invokeTask).DefaultTimeout();
                Assert.Equal("Streaming hub methods must be invoked with the 'HubConnection.StreamAsChannelAsync' method.", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamYieldsItemsAsTheyArrive()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<string>("Foo").DefaultTimeout();

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "1" }).DefaultTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "2" }).DefaultTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, item = "3" }).DefaultTimeout();
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();

                var notifications = await channel.ReadAndCollectAllAsync().DefaultTimeout();

                Assert.Equal(new[] { "1", "2", "3", }, notifications.ToArray());
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On<int, string, float>("Foo", (r1, r2, r3) => handlerCalled.TrySetResult(new object[] { r1, r2, r3 }));

                var args = new object[] { 1, "Foo", 2.0f };
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = args }).DefaultTimeout();

                Assert.Equal(args, await handlerCalled.Task.DefaultTimeout());
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On<int>("Foo", (val) =>
                {
                    handlerCalled.TrySetResult(val);
                });

                hubConnection.Remove("Foo");
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = 1 }).DefaultTimeout();
                var handlerTask = handlerCalled.Task;

                // We expect the handler task to timeout since the handler has been removed with the call to Remove("Foo")
                var ex = Assert.ThrowsAsync<TimeoutException>(async () => await handlerTask.DefaultTimeout(2000));

                // Ensure that the task from the WhenAny is not the handler task
                Assert.False(handlerCalled.Task.IsCompleted);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                var subscription = hubConnection.On<int>("Foo", (val) =>
                {
                    handlerCalled.TrySetResult(val);
                });

                hubConnection.Remove("Foo");
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = 1 }).DefaultTimeout();
                var handlerTask = handlerCalled.Task;

                subscription.Dispose();

                // We expect the handler task to timeout since the handler has been removed with the call to Remove("Foo")
                var ex = Assert.ThrowsAsync<TimeoutException>(async () => await handlerTask.DefaultTimeout(2000));

                // Ensure that the task from the WhenAny is not the handler task
                Assert.False(handlerCalled.Task.IsCompleted);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task AcceptsPingMessages()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);

            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // Send an invocation
                var invokeTask = hubConnection.InvokeAsync("Foo");

                // Receive the ping mid-invocation so we can see that the rest of the flow works fine
                await connection.ReceiveJsonMessage(new { type = 6 }).DefaultTimeout();

                // Receive a completion
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();

                // Ensure the invokeTask completes properly
                await invokeTask.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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

                await connection.ReceiveTextAsync("{").DefaultTimeout();

                Assert.False(task.IsCompleted);

                await connection.ReceiveTextAsync("}").DefaultTimeout();

                Assert.False(task.IsCompleted);

                await connection.ReceiveTextAsync("\u001e").DefaultTimeout();

                await task.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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

                await connection.ReceiveTextAsync(payload).DefaultTimeout();

                await hubConnection.StartAsync().DefaultTimeout();

                var response = await tcs.Task.DefaultTimeout();
                Assert.Equal("hello", response);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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

                await hubConnection.StartAsync().DefaultTimeout();

                await connection.ReceiveTextAsync("{\"type\":1, ").DefaultTimeout();

                Assert.False(tcs.Task.IsCompleted);

                await connection.ReceiveTextAsync("\"target\": \"Echo\", \"arguments\"").DefaultTimeout();

                Assert.False(tcs.Task.IsCompleted);

                await connection.ReceiveTextAsync(":[\"hello\"]}\u001e").DefaultTimeout();

                var response = await tcs.Task.DefaultTimeout();

                Assert.Equal("hello", response);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                var firstPing = await connection.ReadSentTextMessageAsync(ignorePings: false).DefaultTimeout();
                Assert.Equal("{\"type\":6}", firstPing);

                var secondPing = await connection.ReadSentTextMessageAsync(ignorePings: false).DefaultTimeout();
                Assert.Equal("{\"type\":6}", secondPing);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
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
                await hubConnection.StartAsync().DefaultTimeout();

                await Task.Delay(1000);

                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();

                var messages = await connection.ReadAllSentMessagesAsync(ignorePings: false).DefaultTimeout();
                var message = Assert.Single(messages);
                Assert.Equal("{\"type\":7}", message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientCanReturnResult()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On("Result", () => 10);

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"result\":10}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ThrowsWhenMultipleReturningHandlersRegistered()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On("Result", () => 10);
                var ex = Assert.Throws<InvalidOperationException>(
                    () => hubConnection.On("Result", () => 11));
                Assert.Equal("'Result' already has a value returning handler. Multiple return values are not supported.", ex.Message);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientReturnHandlerCanMixWithNonReturnHandler()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                hubConnection.On("Result", () => 40);
                hubConnection.On("Result", tcs.SetResult);

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"result\":40}", invokeMessage);
                await tcs.Task.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientCanThrowErrorResult()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On("Result", int () =>
                {
                    throw new Exception("error from client");
                });

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"error\":\"error from client\"}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultIgnoresErrorWhenLastHandlerSuccessful()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                hubConnection.On("Result", () =>
                {
                    throw new Exception("error from client");
                });

                hubConnection.On("Result", () => 20);

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"result\":20}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultReturnsErrorIfNoHandlerFromClient()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client didn't provide a result.\"}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultReturnsErrorIfNoResultFromClient()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // No result provided
                hubConnection.On("Result", () => { });

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client didn't provide a result.\"}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultReturnsErrorIfCannotParseArgument()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // No result provided
                hubConnection.On("Result", (string _) => 1);

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[15]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client failed to parse argument(s).\"}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultCanReturnNullResult()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // No result provided
                hubConnection.On("Result", object () => null);

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"result\":null}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task ClientResultHandlerDoesNotBlockOtherHandlers()
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                hubConnection.On("Result", async () =>
                {
                    await tcs.Task.DefaultTimeout();
                    return 1;
                });
                hubConnection.On("Other", () => tcs.SetResult());

                await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();
                await connection.ReceiveTextAsync("{\"type\":1,\"target\":\"Other\",\"arguments\":[]}\u001e").DefaultTimeout();

                var invokeMessage = await connection.ReadSentTextMessageAsync().DefaultTimeout();

                Assert.Equal("{\"type\":3,\"invocationId\":\"1\",\"result\":1}", invokeMessage);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }
}

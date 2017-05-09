using System;
using System.Threading.Tasks;
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

                var invokeTask = hubConnection.Invoke("Foo", typeof(void));

                var invokeMessage = await connection.ReadSentTextMessageAsync().OrTimeout();

                Assert.Equal("{\"invocationId\":\"1\",\"type\":1,\"target\":\"Foo\",\"arguments\":[]}", invokeMessage);
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

                var invokeTask = hubConnection.Invoke("Foo", typeof(void));

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
        // This will fail (intentionally) when we support streaming!
        public async Task InvokeFailsWithErrorWhenStreamingItemReceived()
        {
            var connection = new TestConnection();
            var hubConnection = new HubConnection(connection, new JsonHubProtocol(new JsonSerializer()), new LoggerFactory());
            try
            {
                await hubConnection.StartAsync();

                var invokeTask = hubConnection.Invoke<int>("Foo");

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 2, result = 42 }).OrTimeout();

                var ex = await Assert.ThrowsAsync<NotSupportedException>(() => invokeTask).OrTimeout();
                Assert.Equal("Streaming method results are not supported", ex.Message);
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

                hubConnection.On("Foo", new[] { typeof(int), typeof(string), typeof(float) }, (a) => handlerCalled.TrySetResult(a));

                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 1, target = "Foo", arguments = new object[] { 1, "Foo", 2.0f } }).OrTimeout();

                var results = await handlerCalled.Task.OrTimeout();
                Assert.Equal(3, results.Length);
                Assert.Equal(1, results[0]);
                Assert.Equal("Foo", results[1]);
                Assert.Equal(2.0f, results[2]);
            }
            finally
            {
                await hubConnection.DisposeAsync().OrTimeout();
                await connection.DisposeAsync().OrTimeout();
            }
        }
    }
}

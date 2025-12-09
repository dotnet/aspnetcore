// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task CanReturnClientResultToHub()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invocationId = await client.SendHubMessageAsync(new InvocationMessage("1", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.NotNull(invocationMessage.InvocationId);
                var res = 4 + ((long)invocationMessage.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();

                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(9L, completion.Result);
                Assert.Equal(invocationId, completion.InvocationId);
            }
        }
    }

    [Fact]
    public async Task CanReturnClientResultErrorToHub()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedInvokingHubMethod"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(o =>
                {
                    o.EnableDetailedErrors = true;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invocationId = await client.SendHubMessageAsync(new InvocationMessage("1", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.NotNull(invocationMessage.InvocationId);
                await client.SendHubMessageAsync(CompletionMessage.WithError(invocationMessage.InvocationId, "Client error")).DefaultTimeout();

                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal("An unexpected error occurred invoking 'GetClientResult' on the server. HubException: Client error", completion.Error);
                Assert.Equal(invocationId, completion.InvocationId);
            }
        }
    }

    [Fact]
    public async Task ThrowsWhenUsedInOnConnectedAsync()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "ErrorDispatchingHubEvent"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(o =>
                {
                    o.EnableDetailedErrors = true;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedClientResultHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal("Connection closed with an error. InvalidOperationException: Client results inside OnConnectedAsync Hub methods are not allowed.", closeMessage.Error);
            }
        }

        Assert.Single(TestSink.Writes.Where(write => write.EventId.Name == "ErrorDispatchingHubEvent"));
    }

    [Fact]
    public async Task ThrowsWhenUsedInOnDisconnectedAsync()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "ErrorDispatchingHubEvent"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(o =>
                {
                    o.EnableDetailedErrors = true;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnDisconnectedClientResultHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                client.Connection.Abort();

                // Hub asks client for a result, this is an invocation message with an ID
                var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Null(closeMessage.Error);

                var ex = await Assert.ThrowsAsync<IOException>(() => connectionHandlerTask).DefaultTimeout();
                Assert.Equal($"Connection '{client.Connection.ConnectionId}' disconnected.", ex.Message);
            }
        }

        Assert.Single(TestSink.Writes.Where(write => write.EventId.Name == "ErrorDispatchingHubEvent"));
    }

    [Fact]
    public async Task CanUseClientResultsWithIHubContext()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using var client = new TestClient();

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            var context = serviceProvider.GetRequiredService<IHubContext<MethodHub>>();
            var resultTask = context.Clients.Client(client.Connection.ConnectionId).InvokeAsync<int>("GetClientResult", 1, cancellationToken: default);

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal(1L, invocation.Arguments[0]);
            Assert.Equal("GetClientResult", invocation.Target);

            await client.SendHubMessageAsync(CompletionMessage.WithResult(invocation.InvocationId, 2)).DefaultTimeout();

            var result = await resultTask.DefaultTimeout();
            Assert.Equal(2, result);
        }
    }

    [Fact]
    public async Task CanUseClientResultsWithIHubContextT()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<HubT>>();

            using var client = new TestClient();
            var connectionId = client.Connection.ConnectionId;

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            var context = serviceProvider.GetRequiredService<IHubContext<HubT, ITest>>();

            var resultTask = context.Clients.Client(connectionId).GetClientResult(1);

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal(1L, invocation.Arguments[0]);
            Assert.Equal("GetClientResult", invocation.Target);

            await client.SendHubMessageAsync(CompletionMessage.WithResult(invocation.InvocationId, 2)).DefaultTimeout();

            var result = await resultTask.DefaultTimeout();
            Assert.Equal(2, result);
        }
    }

    [Fact]
    public async Task CanReturnClientResultToTypedHubTwoWays()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<HubT>>();

            using var client = new TestClient(invocationBinder: new GetClientResultTwoWaysInvocationBinder());

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

            var invocationId = await client.SendHubMessageAsync(new InvocationMessage(
                invocationId: "1",
                nameof(HubT.GetClientResultTwoWays),
                new object[] { 7, 3 })).DefaultTimeout();

            // Send back "value + 4" to both invocations.
            for (int i = 0; i < 2; i++)
            {
                // Hub asks client for a result, this is an invocation message with an ID.
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.NotNull(invocationMessage.InvocationId);
                var res = 4 + (int)invocationMessage.Arguments[0];
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();
            }

            var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
            Assert.Equal(new ClientResults(11, 7), completion.Result);
        }
    }

    [Fact]
    public async Task ClientResultFromHubDoesNotBlockReceiveLoop()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(o => o.MaximumParallelInvocationsPerClient = 2);
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // block 1 of the 2 parallel invocations
                _ = await client.SendHubMessageAsync(new InvocationMessage("1", nameof(MethodHub.BlockingMethod), Array.Empty<object>())).DefaultTimeout();

                // make multiple invocations which would normally block the invocation processing
                var invocationId = await client.SendHubMessageAsync(new InvocationMessage("2", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();
                var invocationId2 = await client.SendHubMessageAsync(new InvocationMessage("3", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();
                var invocationId3 = await client.SendHubMessageAsync(new InvocationMessage("4", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();

                // Read all 3 invocation messages from the server, shows that the hub processing continued even though parallel invokes is 2
                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                var invocationMessage2 = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                var invocationMessage3 = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());

                Assert.NotNull(invocationMessage.InvocationId);
                Assert.NotNull(invocationMessage2.InvocationId);
                Assert.NotNull(invocationMessage3.InvocationId);
                var res = 4 + ((long)invocationMessage.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();
                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(9L, completion.Result);
                Assert.Equal(invocationId, completion.InvocationId);

                res = 5 + ((long)invocationMessage2.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage2.InvocationId, res)).DefaultTimeout();
                completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(10L, completion.Result);
                Assert.Equal(invocationId2, completion.InvocationId);

                res = 6 + ((long)invocationMessage3.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage3.InvocationId, res)).DefaultTimeout();
                completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(11L, completion.Result);
                Assert.Equal(invocationId3, completion.InvocationId);
            }
        }
    }

    [Fact]
    public async Task ClientResultFromBackgroundThreadInHubMethodWorks()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(tcsService);
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var completionMessage = await client.InvokeAsync(nameof(MethodHub.BackgroundClientResult)).DefaultTimeout();

                tcsService.StartedMethod.SetResult(null);

                var task = await Task.WhenAny(tcsService.EndMethod.Task, client.ReadAsync()).DefaultTimeout();
                if (task == tcsService.EndMethod.Task)
                {
                    await tcsService.EndMethod.Task;
                }
                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await (Task<HubMessage>)task);
                Assert.NotNull(invocationMessage.InvocationId);
                var res = 4 + ((long)invocationMessage.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();

                Assert.Equal(5, await tcsService.EndMethod.Task.DefaultTimeout());

                // Make sure we can still do a Hub invocation and that the semaphore state didn't get messed up
                completionMessage = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                Assert.Equal(43L, completionMessage.Result);
            }
        }
    }

    private class TestBinder : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            return new Type[] { typeof(int) };
        }

        public Type GetReturnType(string invocationId)
        {
            return typeof(string);
        }

        public Type GetStreamItemType(string streamId)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData("MessagePack")]
    [InlineData("Json")]
    public async Task CanCancelClientResultsWithIHubContextT(string protocol)
    {
        IHubProtocol hubProtocol;
        if (string.Equals(protocol, "MessagePack"))
        {
            hubProtocol = new MessagePackHubProtocol();
        }
        else if (string.Equals(protocol, "Json"))
        {
            hubProtocol = new JsonHubProtocol();
        }
        else
        {
            throw new Exception($"Protocol {protocol} not handled by test.");
        }
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<HubT>>();

            using var client = new TestClient(hubProtocol, new TestBinder());
            var connectionId = client.Connection.ConnectionId;

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            var context = serviceProvider.GetRequiredService<IHubContext<HubT, ITest>>();

            var cts = new CancellationTokenSource();
            var resultTask = context.Clients.Client(connectionId).GetClientResultWithCancellation(1, cts.Token);

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal(1, invocation.Arguments[0]);
            Assert.Equal("GetClientResultWithCancellation", invocation.Target);

            cts.Cancel();

            var ex = await Assert.ThrowsAsync<HubException>(() => resultTask).DefaultTimeout();
            Assert.Equal("Invocation canceled by the server.", ex.Message);

            // Sending result after the server is no longer expecting one results in a log and no-ops
            await client.SendHubMessageAsync(CompletionMessage.WithResult(invocation.InvocationId, 2)).DefaultTimeout();

            // Send another message from the client and get a result back to make sure the connection is still active.
            // Regression test for when sending a client result after it was canceled would close the connection
            var completion = await client.InvokeAsync(nameof(HubT.Echo), "test").DefaultTimeout();
            Assert.Equal("test", completion.Result);

            Assert.Contains(TestSink.Writes, c => c.EventId.Name == "UnexpectedCompletion");
        }
    }

    [Fact]
    public async Task CanCancelClientResultsWithIHubContext()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(null, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using var client = new TestClient();
            var connectionId = client.Connection.ConnectionId;

            var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

            // Wait for a connection, or for the endpoint to fail.
            await client.Connected.OrThrowIfOtherFails(connectionHandlerTask).DefaultTimeout();

            var context = serviceProvider.GetRequiredService<IHubContext<MethodHub>>();

            var cts = new CancellationTokenSource();
            var resultTask = context.Clients.Client(connectionId).InvokeAsync<int>(nameof(MethodHub.GetClientResult), 1, cts.Token);

            var message = await client.ReadAsync().DefaultTimeout();
            var invocation = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocation.Arguments);
            Assert.Equal(1L, invocation.Arguments[0]);
            Assert.Equal("GetClientResult", invocation.Target);

            cts.Cancel();

            var ex = await Assert.ThrowsAsync<HubException>(() => resultTask).DefaultTimeout();
            Assert.Equal("Invocation canceled by the server.", ex.Message);

            // Sending result after the server is no longer expecting one results in a log and no-ops
            await client.SendHubMessageAsync(CompletionMessage.WithResult(invocation.InvocationId, 2)).DefaultTimeout();

            // Send another message from the client and get a result back to make sure the connection is still active.
            // Regression test for when sending a client result after it was canceled would close the connection
            var completion = await client.InvokeAsync("Echo", "test").DefaultTimeout();
            Assert.Equal("test", completion.Result);

            Assert.Contains(TestSink.Writes, c => c.EventId.Name == "UnexpectedCompletion");
        }
    }

    [Fact]
    public async Task ClientResultInUploadStreamingMethodWorks()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // Regression test: Use 1 as the stream ID as this is the first ID the server would use for invocation IDs it generates
                // We want to make sure the client result completion doesn't accidentally complete the stream
                var streamId = "1";
                var invocationId = await client.BeginUploadStreamAsync("1", nameof(MethodHub.GetClientResultWithStream), new[] { streamId }, Array.Empty<object>()).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.NotNull(invocationMessage.InvocationId);
                // This check isn't really needed except we want to make sure the regression test mentioned above is still testing the expected scenario
                Assert.Equal("s1", invocationMessage.InvocationId);

                var res = 4 + ((long)invocationMessage.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();

                var completion = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(5L, completion.Result);
                Assert.Equal(invocationId, completion.InvocationId);

                // Make sure we can still do a Hub invocation and that the semaphore state didn't get messed up
                var completionMessage = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                Assert.Equal(43L, completionMessage.Result);
            }
        }
    }

    [Fact]
    public async Task ClientResultInStreamingMethodWorks()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            var invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(int));
            invocationBinder.Setup(b => b.GetParameterTypes(It.IsAny<string>())).Returns(new[] { typeof(int) });
            invocationBinder.Setup(b => b.GetReturnType(It.IsAny<string>())).Returns(typeof(int));
            using (var client = new TestClient(invocationBinder: invocationBinder.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invocationId = await client.SendStreamInvocationAsync(nameof(MethodHub.StreamWithClientResult)).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var invocationMessage = Assert.IsType<InvocationMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.NotNull(invocationMessage.InvocationId);
                var res = 4 + ((int)invocationMessage.Arguments[0]);
                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocationMessage.InvocationId, res)).DefaultTimeout();

                var streamItem = Assert.IsType<StreamItemMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(5, streamItem.Item);
                Assert.Equal(invocationId, streamItem.InvocationId);

                var completionMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(invocationId, completionMessage.InvocationId);

                // Make sure we can still do a Hub invocation and that the semaphore state didn't get messed up
                completionMessage = await client.InvokeAsync(nameof(MethodHub.ValueMethod)).DefaultTimeout();
                Assert.Equal(43, completionMessage.Result);
            }
        }
    }

    private class GetClientResultTwoWaysInvocationBinder : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes(string methodName) => new[] { typeof(int) };
        public Type GetReturnType(string invocationId) => typeof(ClientResults);
        public Type GetStreamItemType(string streamId) => throw new NotImplementedException();
    }
}

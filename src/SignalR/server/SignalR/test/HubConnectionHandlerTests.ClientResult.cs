// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task CanReturnClientResultToHub()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Waiting for a client result blocks the hub dispatcher pipeline, need to allow multiple invocations
                builder.AddSignalR(o => o.MaximumParallelInvocationsPerClient = 2);
            }, LoggerFactory);
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
                // Waiting for a client result blocks the hub dispatcher pipeline, need to allow multiple invocations
                builder.AddSignalR(o =>
                {
                    o.MaximumParallelInvocationsPerClient = 2;
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
                Assert.Equal("An unexpected error occurred invoking 'GetClientResult' on the server. Exception: Client error", completion.Error);
                Assert.Equal(invocationId, completion.InvocationId);
            }
        }
    }

    [Fact]
    public async Task ThrowsWhenParallelHubInvokesNotEnabled()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedInvokingHubMethod"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSignalR(o =>
                {
                    o.MaximumParallelInvocationsPerClient = 1;
                    o.EnableDetailedErrors = true;
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invocationId = await client.SendHubMessageAsync(new InvocationMessage("1", nameof(MethodHub.GetClientResult), new object[] { 5 })).DefaultTimeout();

                // Hub asks client for a result, this is an invocation message with an ID
                var completionMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                Assert.Equal(invocationId, completionMessage.InvocationId);
                Assert.Equal("An unexpected error occurred invoking 'GetClientResult' on the server. InvalidOperationException: Client results inside a Hub method requires HubOptions.MaximumParallelInvocationsPerClient to be greater than 1.",
                    completionMessage.Error);
            }
        }
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
            var resultTask = context.Clients.Single(client.Connection.ConnectionId).InvokeAsync<int>("GetClientResult", 1);

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

            async Task AssertClientResult(Task<int> resultTask)
            {
                var message = await client.ReadAsync().DefaultTimeout();
                var invocation = Assert.IsType<InvocationMessage>(message);

                Assert.Single(invocation.Arguments);
                Assert.Equal(1L, invocation.Arguments[0]);
                Assert.Equal("GetClientResult", invocation.Target);

                await client.SendHubMessageAsync(CompletionMessage.WithResult(invocation.InvocationId, 2)).DefaultTimeout();

                var result = await resultTask.DefaultTimeout();
                Assert.Equal(2, result);
            }

            await AssertClientResult(context.Clients.Single(connectionId).GetClientResult(1));
            await AssertClientResult(context.Clients.Client(connectionId).GetClientResult(1));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task InvocationsWithoutTrackedCancellationDoNotAllocateCancellationSources()
    {
        using var log = StartVerifiableLog();
        var connected = new TaskCompletionSource<HubConnectionContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifetimeManager = new Mock<HubLifetimeManager<MethodHub>>();
        lifetimeManager.Setup(manager => manager.OnConnectedAsync(It.IsAny<HubConnectionContext>()))
            .Callback<HubConnectionContext>(connection => connected.SetResult(connection))
            .Returns(Task.CompletedTask);
        using var serviceProvider = (ServiceProvider)HubConnectionHandlerTestUtils.CreateServiceProvider(
            services => services.AddSingleton(lifetimeManager.Object), LoggerFactory);
        var handler = serviceProvider.GetRequiredService<HubConnectionHandler<MethodHub>>();
        using var client = new TestClient();
        var connectionTask = await client.ConnectAsync(handler).DefaultTimeout();
        var connection = await connected.Task.DefaultTimeout();
        Assert.Null(GetCancellationSources(connection));

        await client.SendHubMessageAsync(new CancelInvocationMessage("missing")).DefaultTimeout();
        var result = await client.InvokeAsync(nameof(MethodHub.Echo), "value").DefaultTimeout();
        Assert.Null(result.Error);
        Assert.Equal("value", result.Result);
        Assert.Null(GetCancellationSources(connection));

        client.Dispose();
        await connectionTask.DefaultTimeout();
        Assert.Null(GetCancellationSources(connection));
    }

    private static ConcurrentDictionary<string, CancellationTokenSource>? GetCancellationSources(HubConnectionContext connection)
    {
        var field = typeof(HubConnectionContext).GetField("_activeRequestCancellationSources", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (ConcurrentDictionary<string, CancellationTokenSource>?)field.GetValue(connection);
    }
}

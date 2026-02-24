// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.InMemory.Tests;

public class InMemoryHubConnectionTests
{
    [Fact]
    public async Task CanConnectAndInvokeHubMethod()
    {
        using var host = await CreateServerHost();
        var serverServices = host.Services;

        await using var connection = CreateHubConnection<EchoHub>(serverServices);

        await connection.StartAsync();

        var result = await connection.InvokeAsync<string>("Echo", "Hello");

        Assert.Equal("Hello", result);

        await connection.StopAsync();
    }

    [Fact]
    public async Task CanReceiveMessagesFromHub()
    {
        using var host = await CreateServerHost();
        var serverServices = host.Services;

        await using var connection = CreateHubConnection<EchoHub>(serverServices);

        var tcs = new TaskCompletionSource<string>();
        connection.On<string>("ReceiveMessage", msg => tcs.SetResult(msg));

        await connection.StartAsync();

        await connection.InvokeAsync("SendMessage", "TestMessage");

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("TestMessage", received);

        await connection.StopAsync();
    }

    [Fact]
    public async Task MultipleClientsCanConnectSimultaneously()
    {
        using var host = await CreateServerHost();
        var serverServices = host.Services;

        await using var connection1 = CreateHubConnection<EchoHub>(serverServices);
        await using var connection2 = CreateHubConnection<EchoHub>(serverServices);

        await connection1.StartAsync();
        await connection2.StartAsync();

        var result1 = await connection1.InvokeAsync<string>("Echo", "Client1");
        var result2 = await connection2.InvokeAsync<string>("Echo", "Client2");

        Assert.Equal("Client1", result1);
        Assert.Equal("Client2", result2);

        await connection1.StopAsync();
        await connection2.StopAsync();
    }

    [Fact]
    public async Task ConnectionCanBeStoppedAndRestarted()
    {
        using var host = await CreateServerHost();
        var serverServices = host.Services;

        await using var connection = CreateHubConnection<EchoHub>(serverServices);

        await connection.StartAsync();
        var result1 = await connection.InvokeAsync<string>("Echo", "First");
        Assert.Equal("First", result1);

        await connection.StopAsync();

        await connection.StartAsync();
        var result2 = await connection.InvokeAsync<string>("Echo", "Second");
        Assert.Equal("Second", result2);
    }

    [Fact]
    public async Task HubOnConnectedAndOnDisconnectedAreCalled()
    {
        using var host = await CreateServerHost();
        var serverServices = host.Services;

        await using var connection = CreateHubConnection<LifecycleHub>(serverServices);

        var connectedTcs = new TaskCompletionSource<string>();
        connection.On<string>("Connected", id => connectedTcs.SetResult(id));

        await connection.StartAsync();

        var connectionId = await connectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(string.IsNullOrEmpty(connectionId));

        await connection.StopAsync();
    }

    private static HubConnection CreateHubConnection<THub>(IServiceProvider serverServices) where THub : class
    {
        return new HubConnectionBuilder()
            .WithInMemoryHub<THub>(serverServices)
            .Build();
    }

    private static async Task<IHost> CreateServerHost()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSignalR();
                services.AddInMemoryHubConnection<EchoHub>(typeof(HubConnectionHandler<EchoHub>));
                services.AddInMemoryHubConnection<LifecycleHub>(typeof(HubConnectionHandler<LifecycleHub>));
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    public class EchoHub : Hub
    {
        public string Echo(string message) => message;

        public async Task SendMessage(string message)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", message);
        }
    }

    public class LifecycleHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests that a SignalR server can use typed clients in a trimmed app.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddSignalR();
AppJsonSerializerContext.AddToJsonHubProtocol(builder.Services);

var app = builder.Build();
app.MapHub<TestHub>("/testhub");
await app.StartAsync().ConfigureAwait(false);

// connect a client and ensure we can invoke a method on the server
var serverUrl = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
var hubConnectionBuilder = new HubConnectionBuilder()
    .WithUrl(serverUrl + "/testhub");
AppJsonSerializerContext.AddToJsonHubProtocol(hubConnectionBuilder.Services);
var connection = hubConnectionBuilder.Build();

var receivedMessageTask = new TaskCompletionSource<string>();
connection.On<string, string>("ReceiveMessage", (user, message) =>
{
    receivedMessageTask.SetResult($"{user}: {message}");
});

await connection.StartAsync().ConfigureAwait(false);

await connection.InvokeAsync("SendMessage", "userA", "my message").ConfigureAwait(false);

var receivedMessage = await receivedMessageTask.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
if (receivedMessage != "userA: my message")
{
    return -1;
}

return 100;

public interface ITestHubClientBase
{
    Task BaseMethod();
    Task Unused(string unused);
}

public interface ITestHubClient : ITestHubClientBase
{
    Task ReceiveMessage(string user, string message);
}

public class TestHub : Hub<ITestHubClient>
{
    public async Task SendMessage(ILogger<TestHub> logger, string user, string message)
    {
        logger.LogInformation("Received message from {user}: {message}", user, message);

        await Clients.Caller.BaseMethod().ConfigureAwait(false);

        await Clients.All.ReceiveMessage(user, message).ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(string))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
    public static void AddToJsonHubProtocol(IServiceCollection services)
    {
        services.Configure<JsonHubProtocolOptions>(o =>
        {
            o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, Default);
        });
    }
}

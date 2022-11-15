// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace SignalRSamples.Hubs;

public class Chat : Hub
{
    public override Task OnConnectedAsync()
    {
        var name = Context.GetHttpContext().Request.Query["name"];
        return Clients.All.SendAsync("Send", $"{name} joined the chat");
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var name = Context.GetHttpContext().Request.Query["name"];
        return Clients.All.SendAsync("Send", $"{name} left the chat");
    }

    public Task Send(string name, string message)
    {
        return Clients.All.SendAsync("Send", $"{name}: {message}");
    }

    public Task SendToOthers(string name, string message)
    {
        return Clients.Others.SendAsync("Send", $"{name}: {message}");
    }

    public Task SendToConnection(string connectionId, string name, string message)
    {
        return Clients.Client(connectionId).SendAsync("Send", $"Private message from {name}: {message}");
    }

    public Task SendToGroup(string groupName, string name, string message)
    {
        return Clients.Group(groupName).SendAsync("Send", $"{name}@{groupName}: {message}");
    }

    public Task SendToOthersInGroup(string groupName, string name, string message)
    {
        return Clients.OthersInGroup(groupName).SendAsync("Send", $"{name}@{groupName}: {message}");
    }

    public async Task JoinGroup(string groupName, string name)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync("Send", $"{name} joined {groupName}");
    }

    public async Task LeaveGroup(string groupName, string name)
    {
        await Clients.Group(groupName).SendAsync("Send", $"{name} left {groupName}");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public Task Echo(string name, string message)
    {
        return Clients.Caller.SendAsync("Send", $"{name}: {message}");
    }

    static async Task SendThunk(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (Microsoft.AspNetCore.SignalR.Protocol.InvocationMessage)message;
        var args = invocation.Arguments;
        try
        {
            await ((Chat)hub).Send((string)args[0], (string)args[1]);
        }
        catch (Exception ex) when (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, "Invoking Send failed"));
            return;
        }
        finally
        {
        }

        if (invocation.InvocationId is not null)
        {
            await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithResult(invocation.InvocationId, null));
        }
    }

    public static void BindHub(Microsoft.AspNetCore.SignalR.IHubDefinition definition)
    {
        // Streaming parameters, tokens and from services
        definition.AddHubMethod("Send", SendThunk);
    }
}

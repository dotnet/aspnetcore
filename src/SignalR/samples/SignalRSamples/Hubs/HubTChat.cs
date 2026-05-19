// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace SignalRSamples.Hubs;

public class HubTChat : Hub<IChatClient>
{
    public override Task OnConnectedAsync()
    {
        var name = Context.GetHttpContext().Request.Query["name"];
        return Clients.All.Send($"{name} joined the chat");
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var name = Context.GetHttpContext().Request.Query["name"];
        return Clients.All.Send($"{name} left the chat");
    }

    public Task Send(string name, string message)
    {
        return Clients.All.Send($"{name}: {message}");
    }

    public Task SendToOthers(string name, string message)
    {
        return Clients.Others.Send($"{name}: {message}");
    }

    public Task SendToGroup(string groupName, string name, string message)
    {
        return Clients.Group(groupName).Send($"{name}@{groupName}: {message}");
    }

    public Task SendToOthersInGroup(string groupName, string name, string message)
    {
        return Clients.OthersInGroup(groupName).Send($"{name}@{groupName}: {message}");
    }

    public async Task JoinGroup(string groupName, string name)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).Send($"{name} joined {groupName}");
    }

    public async Task LeaveGroup(string groupName, string name)
    {
        await Clients.Group(groupName).Send($"{name} left {groupName}");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public Task Echo(string name, string message)
    {
        return Clients.Caller.Send($"{name}: {message}");
    }
}

public interface IChatClient
{
    Task Send(string message);
}

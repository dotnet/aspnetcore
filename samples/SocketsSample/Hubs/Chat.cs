// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SocketsSample.Hubs
{
    public class Chat : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.InvokeAsync("Send", $"{Context.Connection.ConnectionId} joined");
        }

        public override async Task OnDisconnectedAsync()
        {
            await Clients.All.InvokeAsync("Send", $"{Context.Connection.ConnectionId} left");
        }

        public Task Send(string message)
        {
            return Clients.All.InvokeAsync("Send", $"{Context.ConnectionId}: {message}");
        }

        public Task SendToGroup(string groupName, string message)
        {
            return Clients.Group(groupName).InvokeAsync("Send", $"{Context.ConnectionId}@{groupName}: {message}");
        }

        public async Task JoinGroup(string groupName)
        {
            await Clients.Group(groupName).InvokeAsync("Send", $"{Context.Connection.ConnectionId} joined {groupName}");

            await Groups.AddAsync(groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveAsync(groupName);

            await Clients.Group(groupName).InvokeAsync("Send", $"{Context.Connection.ConnectionId} left {groupName}");
        }
    }
}

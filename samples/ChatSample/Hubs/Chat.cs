// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample.Hubs
{
    // TODO: Make this work
    [Authorize]
    public class Chat : Hub
    {
        private static readonly ConcurrentDictionary<string, string> usersOnline = new ConcurrentDictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                Context.Connection.Dispose();
            }

            await Clients.Client(Context.ConnectionId).InvokeAsync("SetUsersOnline", usersOnline);
            usersOnline.TryAdd(Context.ConnectionId, Context.User.Identity.Name);

            await Clients.All.InvokeAsync("OnConnected", Context.ConnectionId, Context.User.Identity.Name);
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            usersOnline.TryRemove(Context.ConnectionId, out var value);
            return Clients.All.InvokeAsync("OnDisconnected", Context.ConnectionId, Context.User.Identity.Name);
        }

        public async Task Send(string message)
        {
            await Clients.All.InvokeAsync("Send", Context.User.Identity.Name, message);
        }
    }
}

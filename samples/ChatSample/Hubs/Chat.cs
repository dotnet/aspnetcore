// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample.Hubs
{
    // TODO: Make this work
    [Authorize]
    public class Chat : Hub
    {
        public override Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                Context.Connection.Dispose();
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            return Task.CompletedTask;
        }

        public async Task Send(string message)
        {
            await Clients.All.InvokeAsync("Send", $"{Context.User.Identity.Name}: {message}");
        }
    }
}

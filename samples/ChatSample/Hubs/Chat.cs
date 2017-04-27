// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ChatSample.Hubs
{
    // TODO: Make this work
    [Authorize]
    public class Chat : HubWithPresence
    {
        public Chat(IUserTracker<Chat> userTracker)
            : base(userTracker)
        {
        }

        public override async Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                Context.Connection.Dispose();
                return;
            }

            await Clients.Client(Context.ConnectionId).InvokeAsync("SetUsersOnline", await GetUsersOnline());
            await base.OnConnectedAsync();
        }

        public override Task OnUserJoined(UserDetails user)
        {
            if (user.ConnectionId != Context.ConnectionId)
            {
                return Clients.Client(Context.ConnectionId).InvokeAsync("UserJoined", user);
            }

            return Task.CompletedTask;
        }

        public override Task OnUserLeft(UserDetails user)
        {
            return Clients.Client(Context.ConnectionId).InvokeAsync("UserLeft", user);
        }

        public async Task Send(string message)
        {
            await Clients.All.InvokeAsync("Send", Context.User.Identity.Name, message);
        }
    }
}

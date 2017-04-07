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
        public Chat(IPresenceManager presenceManager)
            : base(presenceManager)
        {
        }

        public override async Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                Context.Connection.Dispose();
            }

            await Clients.Client(Context.ConnectionId).InvokeAsync("SetUsersOnline", await UsersOnline);
            await base.OnConnectedAsync();
        }

        public override Task OnUserJoined(UserDetails user)
        {
            return Clients.Client(Context.ConnectionId).InvokeAsync("UserJoined", user);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

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
                Context.Connection.Channel.Dispose();
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        public async Task Send(string message)
        {
            await Clients.All.InvokeAsync("Send", $"{Context.User.Identity.Name}: {message}");
        }
    }
}

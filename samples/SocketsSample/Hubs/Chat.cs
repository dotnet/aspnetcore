using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SocketsSample.Hubs
{
    public class Chat : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.InvokeAsync("Send", Context.Connection.ConnectionId + " joined");
        }

        public override async Task OnDisconnectedAsync()
        {
            await Clients.All.InvokeAsync("Send", Context.Connection.ConnectionId + " left");
        }

        public Task Send(string message)
        {
            return Clients.All.InvokeAsync("Send", Context.ConnectionId + ": " + message);
        }
    }
}

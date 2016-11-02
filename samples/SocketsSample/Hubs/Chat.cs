using System;
using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public class Chat : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.Invoke("Send", Context.Connection.ConnectionId + " joined");
        }

        public override async Task OnDisconnectedAsync()
        {
            await Clients.All.Invoke("Send", Context.Connection.ConnectionId + " left");
        }

        public Task Send(string message)
        {
            return Clients.All.Invoke("Send", Context.ConnectionId + ": " + message);
        }
    }
}

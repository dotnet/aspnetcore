using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Company.WebApplication1
{
    public class ChatHub : Hub
    {
        public Task SendMessageToGroup(string username, string groupName, string message)
        {
            return Clients.Group(groupName).SendAsync("Send", $"{username} ({groupName}): {message}");
        }

        public async Task AddToGroup(string username, string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("Send", $"{username} has joined the group {groupName}.");
        }

        public async Task RemoveFromGroup(string username, string groupName)
        {
            await Clients.Group(groupName).SendAsync("Send", $"{username} is leaving the group {groupName}.");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}

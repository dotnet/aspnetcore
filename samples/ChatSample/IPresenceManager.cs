using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;

namespace ChatSample
{
    public class UserDetails
    {
        public UserDetails(string connectionId, string name)
        {
            ConnectionId = connectionId;
            Name = name;
        }

        public string ConnectionId { get; }
        public string Name { get; }
    }

    public interface IPresenceManager
    {
        IEnumerable<UserDetails> UsersOnline { get; }
        Task UserJoined(Connection connection);
        Task UserLeft(Connection connection);
    }
}

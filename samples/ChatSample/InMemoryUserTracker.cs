using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace ChatSample
{
    public class InMemoryUserTracker<THub> : IUserTracker<THub>
    {
        private readonly ConcurrentDictionary<Connection, UserDetails> _usersOnline
            = new ConcurrentDictionary<Connection, UserDetails>();

        public event Action<UserDetails[]> UsersJoined;
        public event Action<UserDetails[]> UsersLeft;

        public Task<IEnumerable<UserDetails>> UsersOnline()
            => Task.FromResult(_usersOnline.Values.AsEnumerable());

        public Task AddUser(Connection connection, UserDetails userDetails)
        {
            _usersOnline.TryAdd(connection, userDetails);
            UsersJoined(new[] { userDetails });

            return Task.CompletedTask;
        }

        public Task RemoveUser(Connection connection)
        {
            if (_usersOnline.TryRemove(connection, out var userDetails))
            {
                UsersLeft(new[] { userDetails });
            }

            return Task.CompletedTask;
        }
    }
}

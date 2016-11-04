
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;

namespace PersisitentConnection
{
    public class PersistentConnectionLifeTimeManager
    {
        private readonly ConnectionList _connectionList = new ConnectionList();

        public void OnConnectedAsync(Connection connection)
        {
            _connectionList.Add(connection);
        }

        public void OnDisconnectedAsync(Connection connection)
        {
            _connectionList.Remove(connection);
        }

        public async Task SendToAllAsync<T>(T data)
        {
            foreach (var connection in _connectionList)
            {
//                var formatType = connection.Metadata.Get<string>("formatType");
                var formatter = new JsonStreamFormatter<T>();
                await formatter.WriteAsync(data, connection.Channel.GetStream());
            }
        }

        public Task InvokeConnectionAsync(string connectionId, object data)
        {
            throw new NotImplementedException();
        }

        public Task InvokeGroupAsync(string groupName, object data)
        {
            throw new NotImplementedException();
        }

        public Task InvokeUserAsync(string userId, object data)
        {
            throw new NotImplementedException();
        }

        public void AddGroupAsync(Connection connection, string groupName)
        {
            var groups = connection.Metadata.GetOrAdd("groups", _ => new HashSet<string>());
            lock (groups)
            {
                groups.Add(groupName);
            }
        }

        public void RemoveGroupAsync(Connection connection, string groupName)
        {
            var groups = connection.Metadata.Get<HashSet<string>>("groups");
            if (groups != null)
            {
                lock (groups)
                {
                    groups.Remove(groupName);
                }
            }
        }
    }
}

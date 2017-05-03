// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets;

namespace SocialWeather
{
    public class PersistentConnectionLifeTimeManager
    {
        private readonly FormatterResolver _formatterResolver;
        private readonly ConnectionList _connectionList = new ConnectionList();

        public PersistentConnectionLifeTimeManager(FormatterResolver formatterResolver)
        {
            _formatterResolver = formatterResolver;
        }

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
                var formatter = _formatterResolver.GetFormatter<T>(connection.Metadata.Get<string>("formatType"));
                var ms = new MemoryStream();
                await formatter.WriteAsync(data, ms);

                var context = (HttpContext)connection.Metadata[typeof(HttpContext)];
                var format =
                    string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                        ? MessageType.Binary
                        : MessageType.Text;

                connection.Transport.Output.TryWrite(new Message(ms.ToArray(), format, endOfMessage: true));
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

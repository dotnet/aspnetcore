// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;

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

        public void OnConnectedAsync(ConnectionContext connection)
        {
            connection.Metadata["groups"] = new HashSet<string>();
            var format = connection.GetHttpContext().Request.Query["formatType"].ToString();
            connection.Metadata["format"] = format;
            if (string.Equals(format, "protobuf", StringComparison.OrdinalIgnoreCase))
            {
                var transferModeFeature = connection.Features.Get<ITransferModeFeature>();
                if (transferModeFeature != null)
                {
                    transferModeFeature.TransferMode = TransferMode.Binary;
                }
            }
            _connectionList.Add(connection);
        }

        public void OnDisconnectedAsync(ConnectionContext connection)
        {
            _connectionList.Remove(connection);
        }

        public async Task SendToAllAsync<T>(T data)
        {
            foreach (var connection in _connectionList)
            {
                var context = connection.GetHttpContext();
                var formatter = _formatterResolver.GetFormatter<T>((string)connection.Metadata["format"]);
                var ms = new MemoryStream();
                await formatter.WriteAsync(data, ms);

                await connection.Transport.Output.WriteAsync(ms.ToArray());
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

        public void AddGroupAsync(ConnectionContext connection, string groupName)
        {
            var groups = (HashSet<string>)connection.Metadata["groups"];
            lock (groups)
            {
                groups.Add(groupName);
            }
        }

        public void RemoveGroupAsync(ConnectionContext connection, string groupName)
        {
            var groups = (HashSet<string>)connection.Metadata["groups"];
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

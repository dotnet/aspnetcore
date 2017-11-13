// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample.EndPoints
{
    public class MessagesEndPoint : EndPoint
    {
        public ConnectionList Connections { get; } = new ConnectionList();

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            Connections.Add(connection);

            await Broadcast($"{connection.ConnectionId} connected ({connection.Metadata[ConnectionMetadataNames.Transport]})");

            try
            {
                while (await connection.Transport.Reader.WaitToReadAsync())
                {
                    if (connection.Transport.Reader.TryRead(out var buffer))
                    {
                        // We can avoid the copy here but we'll deal with that later
                        var text = Encoding.UTF8.GetString(buffer);
                        text = $"{connection.ConnectionId}: {text}";
                        await Broadcast(Encoding.UTF8.GetBytes(text));
                    }
                }
            }
            finally
            {
                Connections.Remove(connection);

                await Broadcast($"{connection.ConnectionId} disconnected ({connection.Metadata[ConnectionMetadataNames.Transport]})");
            }
        }

        private Task Broadcast(string text)
        {
            return Broadcast(Encoding.UTF8.GetBytes(text));
        }

        private Task Broadcast(byte[] payload)
        {
            var tasks = new List<Task>(Connections.Count);

            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Writer.WriteAsync(payload));
            }

            return Task.WhenAll(tasks);
        }
    }
}

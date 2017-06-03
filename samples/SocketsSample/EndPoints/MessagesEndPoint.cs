// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
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
                while (await connection.Transport.Input.WaitToReadAsync())
                {
                    Message message;
                    if (connection.Transport.Input.TryRead(out message))
                    {
                        // We can avoid the copy here but we'll deal with that later
                        var text = Encoding.UTF8.GetString(message.Payload);
                        text = $"{connection.ConnectionId}: {text}";
                        await Broadcast(Encoding.UTF8.GetBytes(text), message.Type);
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
            return Broadcast(Encoding.UTF8.GetBytes(text), MessageType.Text);
        }

        private Task Broadcast(byte[] payload, MessageType format)
        {
            var tasks = new List<Task>(Connections.Count);

            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Output.WriteAsync(new Message(
                    payload,
                    format)));
            }

            return Task.WhenAll(tasks);
        }
    }
}

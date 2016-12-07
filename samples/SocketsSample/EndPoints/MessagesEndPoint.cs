// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample.EndPoints
{
    public class MessagesEndPoint : MessagingEndPoint
    {
        public ConnectionList<MessagingConnection> Connections { get; } = new ConnectionList<MessagingConnection>();

        public override async Task OnConnectedAsync(MessagingConnection connection)
        {
            Connections.Add(connection);

            await Broadcast($"{connection.ConnectionId} connected ({connection.Metadata["transport"]})");

            try
            {
                while (true)
                {
                    using (var message = await connection.Transport.Input.ReadAsync())
                    {
                        // We can avoid the copy here but we'll deal with that later
                        await Broadcast(message.Payload.Buffer, message.MessageFormat, message.EndOfMessage);
                    }
                }
            }
            catch (Exception ex) when (ex.GetType().IsNested && ex.GetType().DeclaringType == typeof(Channel))
            {
                // Gross that we have to catch this this way. See https://github.com/dotnet/corefxlab/issues/1068
            }
            finally
            {
                Connections.Remove(connection);

                await Broadcast($"{connection.ConnectionId} disconnected ({connection.Metadata["transport"]})");
            }
        }

        private Task Broadcast(string text)
        {
            return Broadcast(ReadableBuffer.Create(Encoding.UTF8.GetBytes(text)), Format.Text, endOfMessage: true);
        }

        private Task Broadcast(ReadableBuffer payload, Format format, bool endOfMessage)
        {
            var tasks = new List<Task>(Connections.Count);

            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Output.WriteAsync(new Message(
                    payload.Preserve(),
                    format,
                    endOfMessage)));
            }

            return Task.WhenAll(tasks);
        }
    }
}

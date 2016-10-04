using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class ChatEndPoint : EndPoint
    {
        public override async Task OnConnected(Connection connection)
        {
            await Broadcast($"{connection.ConnectionId} connected ({connection.Metadata["transport"]})");


            while (true)
            {
                var input = await connection.Channel.Input.ReadAsync();
                try
                {
                    if (input.IsEmpty && connection.Channel.Input.Reading.IsCompleted)
                    {
                        break;
                    }

                    // We can avoid the copy here but we'll deal with that later
                    await Broadcast(input.ToArray());
                }
                finally
                {
                    connection.Channel.Input.Advance(input.End);
                }
            }

            await Broadcast($"{connection.ConnectionId} disconnected ({connection.Metadata["transport"]})");
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
                tasks.Add(c.Channel.Output.WriteAsync(payload));
            }

            return Task.WhenAll(tasks);
        }
    }

}

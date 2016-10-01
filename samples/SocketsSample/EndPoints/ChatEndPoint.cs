using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class ChatEndPoint : EndPoint
    {
        private Bus bus = new Bus();

        public override async Task OnConnected(Connection connection)
        {
            using (bus.Subscribe(nameof(ChatEndPoint), message => OnMessage(message, connection)))
            {
                while (true)
                {
                    var input = await connection.Channel.Input.ReadAsync();
                    try
                    {
                        if (input.IsEmpty && connection.Channel.Input.Reading.IsCompleted)
                        {
                            break;
                        }

                        await bus.Publish(nameof(ChatEndPoint), new Message()
                        {
                            Payload = input
                        });
                    }
                    finally
                    {
                        connection.Channel.Input.Advance(input.End);
                    }
                }
            }

            connection.Channel.Input.Complete();
        }

        private async Task OnMessage(Message message, Connection connection)
        {
            var buffer = connection.Channel.Output.Alloc();
            var payload = message.Payload;
            buffer.Append(ref payload);
            await buffer.FlushAsync();
        }
    }

}

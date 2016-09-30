using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;

namespace WebApplication95.EndPoints
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
                    var input = await connection.Input.ReadAsync();
                    try
                    {
                        if (input.IsEmpty && connection.Input.Reading.IsCompleted)
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
                        connection.Input.Advance(input.End);
                    }
                }
            }

            connection.Input.CompleteReader();
        }

        private async Task OnMessage(Message message, Connection connection)
        {
            var buffer = connection.Output.Alloc();
            var payload = message.Payload;
            buffer.Append(ref payload);
            await buffer.FlushAsync();
        }
    }

}

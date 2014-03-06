using Microsoft.Net.WebSockets.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting to start");
            Console.ReadKey();
            RunTestAsync().Wait();
        }

        private static async Task RunTestAsync()
        {
            WebSocketClient client = new WebSocketClient();
            WebSocket socket = await client.ConnectAsync(new Uri("ws://chrross-togo:12345/"), CancellationToken.None);
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            while (true)
            {
                await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(data), CancellationToken.None);
                Console.WriteLine(result.MessageType + ", " + result.Count + ", " + result.EndOfMessage);
                Console.WriteLine(Encoding.UTF8.GetString(data, 0, result.Count));
            }
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }
}

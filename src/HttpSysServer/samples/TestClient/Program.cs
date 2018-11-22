using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
    public class Program
    {
        private const string Address = 
            // "http://localhost:5000/public/1kb.txt";
            "https://localhost:9090/public/1kb.txt";

        public static void Main(string[] args)
        {
            WebRequestHandler handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
            // handler.UseDefaultCredentials = true;
            handler.Credentials = new NetworkCredential(@"redmond\chrross", "passwird");
            HttpClient client = new HttpClient(handler);

            /*
            int completionCount = 0;
            int iterations = 30000;
            for (int i = 0; i < iterations; i++)
            {
                client.GetAsync(Address)
                    .ContinueWith(t => Interlocked.Increment(ref completionCount));
            }

            while (completionCount < iterations)
            {
                Thread.Sleep(10);
            }*/

            while (true)
            {
                Console.WriteLine("Press any key to send request");
                Console.ReadKey();
                var result = client.GetAsync(Address).Result;
                Console.WriteLine(result);
            }

            // RunWebSocketClient().Wait();
            // Console.WriteLine("Done");
            // Console.ReadKey();
        }

        public static async Task RunWebSocketClient()
        {
            ClientWebSocket websocket = new ClientWebSocket();

            string url = "ws://localhost:5000/";
            Console.WriteLine("Connecting to: " + url);
            await websocket.ConnectAsync(new Uri(url), CancellationToken.None);

            string message = "Hello World";
            Console.WriteLine("Sending message: " + message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await websocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] incomingData = new byte[1024];
            WebSocketReceiveResult result = await websocket.ReceiveAsync(new ArraySegment<byte>(incomingData), CancellationToken.None);

            if (result.CloseStatus.HasValue)
            {
                Console.WriteLine("Closed; Status: " + result.CloseStatus + ", " + result.CloseStatusDescription);
            }
            else
            {
                Console.WriteLine("Received message: " + Encoding.UTF8.GetString(incomingData, 0, result.Count));
            }
        }
    }
}

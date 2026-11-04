using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AutobahnTestAppHttpListener
{
    class Program
    {
        // This app only works on Windows 8+
        static int Main(string[] args)
        {
            using (var listener = StartListener())
            {
                if (listener == null)
                {
                    return 1;
                }

                var httpUrl = listener.Prefixes.Single();
                var wsUrl = httpUrl.Replace("http://", "ws://");

                var stopTokenSource = new CancellationTokenSource();
                var task = Run(listener, wsUrl, stopTokenSource.Token);

                Console.CancelKeyPress += (sender, a) =>
                {
                    a.Cancel = true;
                    stopTokenSource.Cancel();
                };

                Console.WriteLine($"HTTP: {httpUrl}");
                Console.WriteLine($"WS  : {wsUrl}");
                Console.WriteLine("Press Ctrl-C to stop...");

                task.Wait();
            }
            return 0;
        }

        private static async Task Run(HttpListener listener, string wsUrl, CancellationToken stopToken)
        {
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var socket = await context.AcceptWebSocketAsync(null);
                        await Echo(socket.WebSocket);
                    }
                    else
                    {
                        using (var writer = new StreamWriter(context.Response.OutputStream))
                        {
                            await writer.WriteLineAsync($"Ready to accept WebSocket request at: {wsUrl}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Request failed: {ex}");
                }
            }
        }

        private static async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        static HttpListener StartListener()
        {
            var port = 49152; // IANA recommends starting at port 49152 for dynamic ports
            while (port < 65535)
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                try
                {
                    listener.Start();
                    return listener;
                }
                catch
                {
                    port++;
                }
            }

            Console.Error.WriteLine("Failed to find a free port!");
            return null;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TestServer;

class Program
{
    static void Main(string[] args)
    {
        RunEchoServer().Wait();
    }

    private static async Task RunEchoServer()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:12345/");
        listener.Start();
        Console.WriteLine("Started");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.Close();
                continue;
            }
            Console.WriteLine("Accepted");

            var wsContext = await context.AcceptWebSocketAsync(null);
            var webSocket = wsContext.WebSocket;

            byte[] buffer = new byte[1024];
            WebSocketReceiveResult received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (received.MessageType != WebSocketMessageType.Close)
            {
                Console.WriteLine($"Echoing {received.Count} bytes received in a {received.MessageType} message; Fin={received.EndOfMessage}");
                // Echo anything we receive
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, received.Count), received.MessageType, received.EndOfMessage, CancellationToken.None);

                received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, CancellationToken.None);

            webSocket.Dispose();
            Console.WriteLine("Finished");
        }
    }
}

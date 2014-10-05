using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.Run(async context =>
            {
                Console.WriteLine("{0} {1}{2}{3}",
                    context.Request.Method,
                    context.Request.PathBase,
                    context.Request.Path,
                    context.Request.QueryString);

                if (context.IsWebSocketRequest)
                {
                    var webSocket = await context.AcceptWebSocketAsync();
                    await EchoAsync(webSocket);
                }
                else
                {
                    context.Response.ContentLength = 11;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello world");
                }
            });
        }

        public async Task EchoAsync(WebSocket webSocket)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            for (; ;)
            {
                var result = await webSocket.ReceiveAsync(
                    buffer,
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    Console.WriteLine("{0}", System.Text.Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                }

                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer.Array, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    CancellationToken.None);
            }
        }
    }
}
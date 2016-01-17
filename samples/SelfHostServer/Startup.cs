using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Server;

namespace SelfHostServer
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            var listener = app.ServerFeatures.Get<WebListener>();
            listener.AuthenticationManager.AuthenticationSchemes = AuthenticationSchemes.AllowAnonymous;

            loggerfactory.AddConsole(LogLevel.Debug);

            app.Run(async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("Hello World: " + DateTime.Now);
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
                    webSocket.Dispose();
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello world from " + context.Request.Host + " at " + DateTime.Now);
                }
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

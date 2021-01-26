using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddConsole());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Echo(context, webSocket, loggerFactory.CreateLogger("Echo"));
                }
                else
                {
                    await next();
                }
            });

            app.UseFileServer();
        }

        private async Task Echo(HttpContext context, WebSocket webSocket, ILogger logger)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
            LogFrame(logger, webSocket, result, buffer);
            while (result.MessageType != WebSocketMessageType.Close)
            {
                // If the client send "ServerClose", then they want a server-originated close to occur
                string content = "<<binary>>";
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    content = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (content.Equals("ServerClose"))
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing from Server", CancellationToken.None);
                        logger.LogDebug($"Sent Frame Close: {WebSocketCloseStatus.NormalClosure} Closing from Server");
                        return;
                    }
                    else if (content.Equals("ServerAbort"))
                    {
                        context.Abort();
                    }
                }

                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                logger.LogDebug($"Sent Frame {result.MessageType}: Len={result.Count}, Fin={result.EndOfMessage}: {content}");

                result = await webSocket.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
                LogFrame(logger, webSocket, result, buffer);
            }
            await webSocket.CloseAsync(webSocket.CloseStatus.Value, webSocket.CloseStatusDescription, CancellationToken.None);
        }

        private void LogFrame(ILogger logger, WebSocket webSocket, ValueWebSocketReceiveResult frame, byte[] buffer)
        {
            var close = frame.MessageType == WebSocketMessageType.Close;
            string message;
            if (close)
            {
                message = $"Close: {webSocket.CloseStatus.Value} {webSocket.CloseStatusDescription}";
            }
            else
            {
                string content = "<<binary>>";
                if (frame.MessageType == WebSocketMessageType.Text)
                {
                    content = Encoding.UTF8.GetString(buffer, 0, frame.Count);
                }
                message = $"{frame.MessageType}: Len={frame.Count}, Fin={frame.EndOfMessage}: {content}";
            }
            logger.LogDebug("Received Frame " + message);
        }
    }
}

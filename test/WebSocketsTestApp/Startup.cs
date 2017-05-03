// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebSockets.Internal;

namespace WebSocketsTestApp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<PipeFactory>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, PipeFactory PipeFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSocketConnections(PipeFactory);

            app.Use(async (context, next) =>
            {
                var webSocketConnectionFeature = context.Features.Get<IHttpWebSocketConnectionFeature>();
                if (webSocketConnectionFeature != null && webSocketConnectionFeature.IsWebSocketRequest)
                {
                    using (var webSocket = await webSocketConnectionFeature.AcceptWebSocketConnectionAsync(new WebSocketAcceptContext()))
                    {
                        await Echo(context, webSocket, loggerFactory.CreateLogger("Echo"));
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseFileServer();
        }

        private async Task Echo(HttpContext context, IWebSocketConnection webSocket, ILogger logger)
        {
            var lastFrameOpcode = WebSocketOpcode.Continuation;
            var closeResult = await webSocket.ExecuteAsync(frame =>
            {
                if (frame.Opcode == WebSocketOpcode.Ping || frame.Opcode == WebSocketOpcode.Pong)
                {
                    // Already handled
                    return Task.CompletedTask;
                }

                LogFrame(logger, lastFrameOpcode, ref frame);

                // If the client send "ServerClose", then they want a server-originated close to occur
                string content = "<<binary>>";
                if (frame.Opcode == WebSocketOpcode.Text)
                {
                    // Slooooow
                    content = Encoding.UTF8.GetString(frame.Payload.ToArray());
                    if (content.Equals("ServerClose"))
                    {
                        logger.LogDebug($"Sending Frame Close: {WebSocketCloseStatus.NormalClosure} Closing from Server");
                        return webSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure, "Closing from Server"));
                    }
                    else if (content.Equals("ServerAbort"))
                    {
                        context.Abort();
                    }
                }

                if (frame.Opcode != WebSocketOpcode.Continuation)
                {
                    lastFrameOpcode = frame.Opcode;
                }
                logger.LogDebug($"Sending {frame.Opcode}: Len={frame.Payload.Length}, Fin={frame.EndOfMessage}: {content}");
                return webSocket.SendAsync(frame);
            });

            if (webSocket.State == WebSocketConnectionState.CloseReceived)
            {
                // Close the connection from our end
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);
                logger.LogDebug("Socket closed");
            }
            else if (webSocket.State != WebSocketConnectionState.Closed)
            {
                logger.LogError("WebSocket closed but not closed?");
            }
        }

        private void LogFrame(ILogger logger, WebSocketOpcode lastFrameOpcode, ref WebSocketFrame frame)
        {
            var opcode = frame.Opcode;
            if (opcode == WebSocketOpcode.Continuation)
            {
                opcode = lastFrameOpcode;
            }

            logger.LogDebug($"Received {frame.Opcode} frame (FIN={frame.EndOfMessage}, LEN={frame.Payload.Length})");
        }
    }
}

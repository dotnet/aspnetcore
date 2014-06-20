// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNet;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Server.WebListener;
using Microsoft.Net.Server;

namespace SelfHostServer
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            var info = (ServerInformation)app.Server;
            info.Listener.AuthenticationManager.AuthenticationTypes = AuthenticationTypes.AllowAnonymous;

            app.Run(async context =>
            {
                if (context.IsWebSocketRequest)
                {
                    Console.WriteLine("WebSocket");
                    byte[] bytes = Encoding.ASCII.GetBytes("Hello World: " + DateTime.Now);
                    WebSocket webSocket = await context.AcceptWebSocketAsync();
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
                    webSocket.Dispose();
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello world");
                }
            });
        }
    }
}

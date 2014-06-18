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
using System.Threading.Tasks;
using Microsoft.Net.Server;

namespace HelloWorld
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (WebListener listener = new WebListener())
            {
                listener.UrlPrefixes.Add(UrlPrefix.Create("http://localhost:8080"));
                listener.Start();

                Console.WriteLine("Running...");
                while (true)
                {
                    RequestContext context = await listener.GetContextAsync();
                    Console.WriteLine("Accepted");

                    // Context:
                    // context.User;
                    // context.DisconnectToken
                    // context.Dispose()
                    // context.Abort();

                    // Request
                    // context.Request.ProtocolVersion
                    // context.Request.IsLocal
                    // context.Request.Headers // TODO: Header helpers?
                    // context.Request.Method
                    // context.Request.Body
                    // Content-Length - long?
                    // Content-Type - string
                    // IsSecureConnection
                    // HasEntityBody

                    // TODO: Request fields
                    // Content-Encoding - Encoding
                    // Host
                    // Client certs - GetCertAsync, CertErrors
                    // Cookies
                    // KeepAlive
                    // QueryString (parsed)
                    // RequestTraceIdentifier
                    // RawUrl
                    // URI
                    // IsWebSocketRequest
                    // LocalEndpoint vs LocalIP & LocalPort
                    // RemoteEndpoint vs RemoteIP & RemotePort
                    // AcceptTypes string[]
                    // ServiceName
                    // TransportContext

                    // Response
                    byte[] bytes = Encoding.ASCII.GetBytes("Hello World: " + DateTime.Now);

                    if (context.IsWebSocketRequest)
                    {
                        Console.WriteLine("WebSocket");
                        WebSocket webSocket = await context.AcceptWebSocketAsync();
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
                        webSocket.Dispose();
                    }
                    else
                    {
                        Console.WriteLine("Hello World");
                        context.Response.ContentLength = bytes.Length;
                        context.Response.ContentType = "text/plain";

                        context.Response.Body.Write(bytes, 0, bytes.Length);
                        context.Dispose();
                    }
                }
            }
        }
    }
}
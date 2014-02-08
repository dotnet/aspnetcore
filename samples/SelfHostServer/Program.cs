// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.AspNet.Server.WebListener;
using Microsoft.Owin.Hosting;
using Owin;

namespace SelfHostServer
{
    // http://owin.org/extensions/owin-WebSocket-Extension-v0.4.0.htm
    using WebSocketAccept = Action<IDictionary<string, object>, // options
        Func<IDictionary<string, object>, Task>>; // callback
    using WebSocketCloseAsync =
        Func<int /* closeStatus */,
            string /* closeDescription */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveAsync =
        Func<ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task<Tuple<int /* messageType */,
                bool /* endOfMessage */,
                int /* count */>>>;
    using WebSocketReceiveResult = Tuple<int, // type
        bool, // end of message?
        int>; // count
    using WebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;

    public class Program
    {
        private static byte[] Data = new byte[1024];

        public static void Main(string[] args)
        {
            using (WebApp.Start<Program>(new StartOptions(
                // "http://localhost:5000/"
                "https://localhost:9090/"
                )
            {
                ServerFactory = "Microsoft.AspNet.Server.WebListener"
            }))
            {
                Console.WriteLine("Running, press any key to exit");
                // System.Diagnostics.Process.Start("http://localhost:5000/");
                Console.ReadKey();
            }
        }
        
        public void Configuration(IAppBuilder app)
        {
            OwinWebListener listener = (OwinWebListener)app.Properties["Microsoft.AspNet.Server.WebListener.OwinWebListener"];
            listener.AuthenticationManager.AuthenticationTypes =
                AuthenticationType.Basic |
                AuthenticationType.Digest |
                AuthenticationType.Negotiate |
                AuthenticationType.Ntlm |
                AuthenticationType.Kerberos;

            app.Use((context, next) =>
            {
                Console.WriteLine("Request: " + context.Request.Uri);
                return next();
            });
            app.Use((context, next) =>
                {
                    if (context.Request.User == null)
                    {
                        context.Response.StatusCode = 401;
                        return Task.FromResult(0);
                    }
                    else
                    {
                        Console.WriteLine(context.Request.User.Identity.AuthenticationType);
                    }
                    return next();
                });
            app.UseWebSockets();
            app.Use(UpgradeToWebSockets);
            app.Run(Invoke);
        }
        
        public Task Invoke(IOwinContext context)
        {
            context.Response.ContentLength = Data.Length;
            return context.Response.WriteAsync(Data);
        }

        // Run once per request
        private Task UpgradeToWebSockets(IOwinContext context, Func<Task> next)
        {
            WebSocketAccept accept = context.Get<WebSocketAccept>("websocket.Accept");
            if (accept == null)
            {
                // Not a websocket request
                return next();
            }

            accept(null, WebSocketEcho);

            return Task.FromResult<object>(null);
        }

        private async Task WebSocketEcho(IDictionary<string, object> websocketContext)
        {
            var sendAsync = (WebSocketSendAsync)websocketContext["websocket.SendAsync"];
            var receiveAsync = (WebSocketReceiveAsync)websocketContext["websocket.ReceiveAsync"];
            var closeAsync = (WebSocketCloseAsync)websocketContext["websocket.CloseAsync"];
            var callCancelled = (CancellationToken)websocketContext["websocket.CallCancelled"];

            byte[] buffer = new byte[1024];
            WebSocketReceiveResult received = await receiveAsync(new ArraySegment<byte>(buffer), callCancelled);

            object status;
            while (!websocketContext.TryGetValue("websocket.ClientCloseStatus", out status) || (int)status == 0)
            {
                // Echo anything we receive
                await sendAsync(new ArraySegment<byte>(buffer, 0, received.Item3), received.Item1, received.Item2, callCancelled);

                received = await receiveAsync(new ArraySegment<byte>(buffer), callCancelled);
            }

            await closeAsync((int)websocketContext["websocket.ClientCloseStatus"], (string)websocketContext["websocket.ClientCloseDescription"], callCancelled);
        }
    }
}

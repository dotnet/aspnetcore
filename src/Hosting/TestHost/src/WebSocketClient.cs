// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.TestHost
{
    public class WebSocketClient
    {
        private readonly ApplicationWrapper _application;
        private readonly PathString _pathBase;

        internal WebSocketClient(PathString pathBase, ApplicationWrapper application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));

            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;

            SubProtocols = new List<string>();
        }

        public IList<string> SubProtocols
        {
            get;
            private set;
        }

        public Action<HttpRequest> ConfigureRequest
        {
            get;
            set;
        }

        internal bool AllowSynchronousIO { get; set; }
        internal bool PreserveExecutionContext { get; set; }

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            WebSocketFeature webSocketFeature = null;
            var contextBuilder = new HttpContextBuilder(_application, AllowSynchronousIO, PreserveExecutionContext);
            contextBuilder.Configure((context, reader) =>
            {
                var request = context.Request;
                var scheme = uri.Scheme;
                scheme = (scheme == "ws") ? "http" : scheme;
                scheme = (scheme == "wss") ? "https" : scheme;
                request.Scheme = scheme;
                if (!request.Host.HasValue)
                {
                    request.Host = uri.IsDefaultPort
                        ? new HostString(HostString.FromUriComponent(uri).Host)
                        : HostString.FromUriComponent(uri);
                }
                request.Path = PathString.FromUriComponent(uri);
                request.PathBase = PathString.Empty;
                if (request.Path.StartsWithSegments(_pathBase, out var remainder))
                {
                    request.Path = remainder;
                    request.PathBase = _pathBase;
                }
                request.QueryString = QueryString.FromUriComponent(uri);
                request.Headers.Add(HeaderNames.Connection, new string[] { "Upgrade" });
                request.Headers.Add(HeaderNames.Upgrade, new string[] { "websocket" });
                request.Headers.Add(HeaderNames.SecWebSocketVersion, new string[] { "13" });
                request.Headers.Add(HeaderNames.SecWebSocketKey, new string[] { CreateRequestKey() });
                if (SubProtocols.Any())
                {
                    request.Headers.Add(HeaderNames.SecWebSocketProtocol, SubProtocols.ToArray());
                }

                request.Body = Stream.Null;

                // WebSocket
                webSocketFeature = new WebSocketFeature(context);
                context.Features.Set<IHttpWebSocketFeature>(webSocketFeature);

                ConfigureRequest?.Invoke(context.Request);
            });

            var httpContext = await contextBuilder.SendAsync(cancellationToken);

            if (httpContext.Response.StatusCode != StatusCodes.Status101SwitchingProtocols)
            {
                throw new InvalidOperationException("Incomplete handshake, status code: " + httpContext.Response.StatusCode);
            }
            if (webSocketFeature.ClientWebSocket == null)
            {
                throw new InvalidOperationException("Incomplete handshake");
            }

            return webSocketFeature.ClientWebSocket;
        }

        private string CreateRequestKey()
        {
            byte[] data = new byte[16];
            RandomNumberGenerator.Fill(data);
            return Convert.ToBase64String(data);
        }

        private class WebSocketFeature : IHttpWebSocketFeature
        {
            private readonly HttpContext _httpContext;

            public WebSocketFeature(HttpContext context)
            {
                _httpContext = context;
            }

            bool IHttpWebSocketFeature.IsWebSocketRequest => true;

            public WebSocket ClientWebSocket { get; private set; }

            public WebSocket ServerWebSocket { get; private set; }

            async Task<WebSocket> IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context)
            {
                var websockets = TestWebSocket.CreatePair(context.SubProtocol);
                if (_httpContext.Response.HasStarted)
                {
                    throw new InvalidOperationException("The response has already started");
                }

                _httpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                ClientWebSocket = websockets.Item1;
                ServerWebSocket = websockets.Item2;
                await _httpContext.Response.Body.FlushAsync(_httpContext.RequestAborted); // Send headers to the client
                return ServerWebSocket;
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Context = Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNetCore.TestHost
{
    public class WebSocketClient
    {
        private readonly IHttpApplication<Context> _application;
        private readonly PathString _pathBase;

        internal WebSocketClient(PathString pathBase, IHttpApplication<Context> application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            
            _application = application;

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

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            var state = new RequestState(uri, _pathBase, cancellationToken, _application);

            if (ConfigureRequest != null)
            {
                ConfigureRequest(state.Context.HttpContext.Request);
            }

            // Async offload, don't let the test code block the caller.
            var offload = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await _application.ProcessRequestAsync(state.Context);
                    state.PipelineComplete();
                    state.ServerCleanup(exception: null);
                }
                catch (Exception ex)
                {
                    state.PipelineFailed(ex);
                    state.ServerCleanup(ex);
                }
                finally
                {
                    state.Dispose();
                }
            });

            return await state.WebSocketTask;
        }

        private class RequestState : IDisposable, IHttpWebSocketFeature
        {
            private readonly IHttpApplication<Context> _application;
            private TaskCompletionSource<WebSocket> _clientWebSocketTcs;
            private CancellationTokenRegistration _cancellationTokenRegistration;
            private WebSocket _serverWebSocket;

            public Context Context { get; private set; }
            public Task<WebSocket> WebSocketTask { get { return _clientWebSocketTcs.Task; } }

            public RequestState(Uri uri, PathString pathBase, CancellationToken cancellationToken, IHttpApplication<Context> application)
            {
                _clientWebSocketTcs = new TaskCompletionSource<WebSocket>();
                _cancellationTokenRegistration = cancellationToken.Register(
                    () => _clientWebSocketTcs.TrySetCanceled(cancellationToken));
                _application = application;

                // HttpContext
                var contextFeatures = new FeatureCollection();
                contextFeatures.Set<IHttpRequestFeature>(new RequestFeature());
                contextFeatures.Set<IHttpResponseFeature>(new ResponseFeature());
                Context = _application.CreateContext(contextFeatures);
                var httpContext = Context.HttpContext;

                // Request
                var request = httpContext.Request;
                request.Protocol = "HTTP/1.1";
                var scheme = uri.Scheme;
                scheme = (scheme == "ws") ? "http" : scheme;
                scheme = (scheme == "wss") ? "https" : scheme;
                request.Scheme = scheme;
                request.Method = "GET";
                var fullPath = PathString.FromUriComponent(uri);
                PathString remainder;
                if (fullPath.StartsWithSegments(pathBase, out remainder))
                {
                    request.PathBase = pathBase;
                    request.Path = remainder;
                }
                else
                {
                    request.PathBase = PathString.Empty;
                    request.Path = fullPath;
                }
                request.QueryString = QueryString.FromUriComponent(uri);
                request.Headers.Add("Connection", new string[] { "Upgrade" });
                request.Headers.Add("Upgrade", new string[] { "websocket" });
                request.Headers.Add("Sec-WebSocket-Version", new string[] { "13" });
                request.Headers.Add("Sec-WebSocket-Key", new string[] { CreateRequestKey() });
                request.Body = Stream.Null;

                // Response
                var response = httpContext.Response;
                response.Body = Stream.Null;
                response.StatusCode = 200;

                // WebSocket
                httpContext.Features.Set<IHttpWebSocketFeature>(this);
            }

            public void PipelineComplete()
            {
                PipelineFailed(new InvalidOperationException("Incomplete handshake, status code: " + Context.HttpContext.Response.StatusCode));
            }

            public void PipelineFailed(Exception ex)
            {
                _clientWebSocketTcs.TrySetException(new InvalidOperationException("The websocket was not accepted.", ex));
            }

            public void Dispose()
            {
                if (_serverWebSocket != null)
                {
                    _serverWebSocket.Dispose();
                }
            }

            internal void ServerCleanup(Exception exception)
            {
                _application.DisposeContext(Context, exception);
            }

            private string CreateRequestKey()
            {
                byte[] data = new byte[16];
                var rng = RandomNumberGenerator.Create();
                rng.GetBytes(data);
                return Convert.ToBase64String(data);
            }

            bool IHttpWebSocketFeature.IsWebSocketRequest
            {
                get
                {
                    return true;
                }
            }

            Task<WebSocket> IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context)
            {
                var websockets = TestWebSocket.CreatePair(context.SubProtocol);
                if (_clientWebSocketTcs.TrySetResult(websockets.Item1))
                {
                    Context.HttpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
                    _serverWebSocket = websockets.Item2;
                    return Task.FromResult(_serverWebSocket);
                }
                else
                {
                    Context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    websockets.Item1.Dispose();
                    websockets.Item2.Dispose();
                    return _clientWebSocketTcs.Task; // Canceled or Faulted - no result
                }
            }
        }
    }
}
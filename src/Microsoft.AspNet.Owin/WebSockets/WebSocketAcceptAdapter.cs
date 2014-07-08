// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using WebSocketAccept =
        Action
        <
            IDictionary<string, object>, // WebSocket Accept parameters
            Func // WebSocketFunc callback
            <
                IDictionary<string, object>, // WebSocket environment
                Task // Complete
            >
        >;
    using WebSocketAcceptAlt =
        Func
        <
            IWebSocketAcceptContext, // WebSocket Accept parameters
            Task<WebSocket>
        >;

    public class WebSocketAcceptAdapter
    {
        private IDictionary<string, object> _env;
        private WebSocketAcceptAlt _accept;
        private AppFunc _callback;
        private IDictionary<string, object> _options;

        public WebSocketAcceptAdapter(IDictionary<string, object> env, WebSocketAcceptAlt accept)
	    {
            _env = env;
            _accept = accept;
        }

        private void AcceptWebSocket(IDictionary<string, object> options, AppFunc callback)
        {
            _options = options;
            _callback = callback;
            _env[OwinConstants.ResponseStatusCode] = 101;
        }

        public static AppFunc AdaptWebSockets(AppFunc next)
        {
            return async environment =>
            {
                object accept;
                if (environment.TryGetValue(OwinConstants.WebSocket.AcceptAlt, out accept) && accept is WebSocketAcceptAlt)
                {
                    var adapter = new WebSocketAcceptAdapter(environment, (WebSocketAcceptAlt)accept);

                    environment[OwinConstants.WebSocket.Accept] = new WebSocketAccept(adapter.AcceptWebSocket);
                    await next(environment);
                    if ((int)environment[OwinConstants.ResponseStatusCode] == 101 && adapter._callback != null)
                    {
                        IWebSocketAcceptContext acceptContext = null;
                        object obj;
                        if (adapter._options.TryGetValue(typeof(IWebSocketAcceptContext).FullName, out obj))
                        {
                            acceptContext = obj as IWebSocketAcceptContext;
                        }
                        else if (adapter._options != null)
                        {
                            acceptContext = new OwinWebSocketAcceptContext(adapter._options);
                        }

                        var webSocket = await adapter._accept(acceptContext);
                        var webSocketAdapter = new WebSocketAdapter(webSocket, (CancellationToken)environment[OwinConstants.CallCancelled]);
                        await adapter._callback(webSocketAdapter.Environment);
                        await webSocketAdapter.CleanupAsync();
                    }
                }
                else
                {
                    await next(environment);
                }
            };
        }
    }
}
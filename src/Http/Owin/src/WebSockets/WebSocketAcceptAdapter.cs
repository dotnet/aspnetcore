// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Owin;

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
        WebSocketAcceptContext, // WebSocket Accept parameters
        Task<WebSocket>
    >;

/// <summary>
/// This adapts the ASP.NET Core WebSocket Accept flow to match the OWIN WebSocket accept flow.
/// This enables OWIN based components to use WebSockets on ASP.NET Core servers.
/// </summary>
public class WebSocketAcceptAdapter
{
    private readonly IDictionary<string, object> _env;
    private readonly WebSocketAcceptAlt _accept;
    private AppFunc _callback;
    private IDictionary<string, object> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="WebSocketAcceptAdapter"/> for an OWIN environment.
    /// </summary>
    /// <param name="env">The OWIN environment.</param>
    /// <param name="accept">WebSocket accept delegate.</param>
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

    /// <summary>
    /// Adapt web sockets to OWIN.
    /// </summary>
    /// <param name="next">The next OWIN app delegate.</param>
    /// <returns>An OWIN app delegate.</returns>
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
                    WebSocketAcceptContext acceptContext = null;
                    object obj;
                    if (adapter._options != null && adapter._options.TryGetValue(typeof(WebSocketAcceptContext).FullName, out obj))
                    {
                        acceptContext = obj as WebSocketAcceptContext;
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

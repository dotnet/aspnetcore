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
/// This adapts the OWIN WebSocket accept flow to match the ASP.NET Core WebSocket Accept flow.
/// This enables ASP.NET Core components to use WebSockets on OWIN based servers.
/// </summary>
public class OwinWebSocketAcceptAdapter
{
    private readonly WebSocketAccept _owinWebSocketAccept;
    private readonly TaskCompletionSource<int> _requestTcs = new TaskCompletionSource<int>();
    private readonly TaskCompletionSource<WebSocket> _acceptTcs = new TaskCompletionSource<WebSocket>();
    private readonly TaskCompletionSource<int> _upstreamWentAsync = new TaskCompletionSource<int>();
    private string _subProtocol;

    private OwinWebSocketAcceptAdapter(WebSocketAccept owinWebSocketAccept)
    {
        _owinWebSocketAccept = owinWebSocketAccept;
    }

    private Task RequestTask { get { return _requestTcs.Task; } }
    private Task UpstreamTask { get; set; }
    private TaskCompletionSource<int> UpstreamWentAsyncTcs { get { return _upstreamWentAsync; } }

    private async Task<WebSocket> AcceptWebSocketAsync(WebSocketAcceptContext context)
    {
        IDictionary<string, object> options = null;
        if (context is OwinWebSocketAcceptContext)
        {
            var acceptContext = context as OwinWebSocketAcceptContext;
            options = acceptContext.Options;
            _subProtocol = acceptContext.SubProtocol;
        }
        else if (context?.SubProtocol != null)
        {
            options = new Dictionary<string, object>(1)
                {
                    { OwinConstants.WebSocket.SubProtocol, context.SubProtocol }
                };
            _subProtocol = context.SubProtocol;
        }

        // Accept may have been called synchronously on the original request thread, we might not have a task yet. Go async.
        await _upstreamWentAsync.Task;

        _owinWebSocketAccept(options, OwinAcceptCallback);
        _requestTcs.TrySetResult(0); // Let the pipeline unwind.

        return await _acceptTcs.Task;
    }

    private Task OwinAcceptCallback(IDictionary<string, object> webSocketContext)
    {
        _acceptTcs.TrySetResult(new OwinWebSocketAdapter(webSocketContext, _subProtocol));
        return UpstreamTask;
    }

    // Make sure declined websocket requests complete. This is a no-op for accepted websocket requests.
    private void EnsureCompleted(Task task)
    {
        if (task.IsCanceled)
        {
            _requestTcs.TrySetCanceled();
        }
        else if (task.IsFaulted)
        {
            _requestTcs.TrySetException(task.Exception);
        }
        else
        {
            _requestTcs.TrySetResult(0);
        }
    }

    // Order of operations:
    // 1. A WebSocket handshake request is received by the middleware.
    // 2. The middleware inserts an alternate Accept signature into the OWIN environment.
    // 3. The middleware invokes Next and stores Next's Task locally. It then returns an alternate Task to the server.
    // 4. The OwinFeatureCollection adapts the alternate Accept signature to IHttpWebSocketFeature.AcceptAsync.
    // 5. A component later in the pipeline invokes IHttpWebSocketFeature.AcceptAsync (mapped to AcceptWebSocketAsync).
    // 6. The middleware calls the OWIN Accept, providing a local callback, and returns an incomplete Task.
    // 7. The middleware completes the alternate Task it returned from Invoke, telling the server that the request pipeline has completed.
    // 8. The server invokes the middleware's callback, which creates a WebSocket adapter and completes the original Accept Task with it.
    // 9. The middleware waits while the application uses the WebSocket, where the end is signaled by the Next's Task completion.
    //
    /// <summary>
    /// Adapt web sockets to OWIN.
    /// </summary>
    /// <param name="next">The next OWIN app delegate.</param>
    /// <returns>An OWIN app delegate.</returns>
    public static AppFunc AdaptWebSockets(AppFunc next)
    {
        return environment =>
        {
            object accept;
            if (environment.TryGetValue(OwinConstants.WebSocket.Accept, out accept) && accept is WebSocketAccept)
            {
                var adapter = new OwinWebSocketAcceptAdapter((WebSocketAccept)accept);

                environment[OwinConstants.WebSocket.AcceptAlt] = new WebSocketAcceptAlt(adapter.AcceptWebSocketAsync);

                try
                {
                    adapter.UpstreamTask = next(environment);
                    adapter.UpstreamWentAsyncTcs.TrySetResult(0);
                    adapter.UpstreamTask.ContinueWith(adapter.EnsureCompleted, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    adapter.UpstreamWentAsyncTcs.TrySetException(ex);
                    throw;
                }

                return adapter.RequestTask;
            }
            else
            {
                return next(environment);
            }
        };
    }
}

# Microsoft.AspNetCore.Http.Connections.Internal.Transports

``` diff
-namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports {
 {
-    public interface IHttpTransport {
 {
-        Task ProcessRequestAsync(HttpContext context, CancellationToken token);

-    }
-    public class LongPollingTransport : IHttpTransport {
 {
-        public LongPollingTransport(CancellationToken timeoutToken, PipeReader application, ILoggerFactory loggerFactory);

-        public Task ProcessRequestAsync(HttpContext context, CancellationToken token);

-    }
-    public class ServerSentEventsTransport : IHttpTransport {
 {
-        public ServerSentEventsTransport(PipeReader application, string connectionId, ILoggerFactory loggerFactory);

-        public Task ProcessRequestAsync(HttpContext context, CancellationToken token);

-    }
-    public class WebSocketsTransport : IHttpTransport {
 {
-        public WebSocketsTransport(WebSocketOptions options, IDuplexPipe application, HttpConnectionContext connection, ILoggerFactory loggerFactory);

-        public Task ProcessRequestAsync(HttpContext context, CancellationToken token);

-        public Task ProcessSocketAsync(WebSocket socket);

-    }
-}
```


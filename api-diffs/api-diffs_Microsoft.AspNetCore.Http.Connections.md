# Microsoft.AspNetCore.Http.Connections

``` diff
 namespace Microsoft.AspNetCore.Http.Connections {
     public class AvailableTransport {
         public AvailableTransport();
         public IList<string> TransferFormats { get; set; }
         public string Transport { get; set; }
     }
+    public class ConnectionOptions {
+        public ConnectionOptions();
+        public Nullable<TimeSpan> DisconnectTimeout { get; set; }
+    }
+    public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions> {
+        public static TimeSpan DefaultDisconectTimeout;
+        public ConnectionOptionsSetup();
+        public void Configure(ConnectionOptions options);
+    }
     public class ConnectionsRouteBuilder {
         public void MapConnectionHandler<TConnectionHandler>(PathString path) where TConnectionHandler : ConnectionHandler;
         public void MapConnectionHandler<TConnectionHandler>(PathString path, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler;
         public void MapConnections(PathString path, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure);
         public void MapConnections(PathString path, Action<IConnectionBuilder> configure);
     }
     public static class HttpConnectionContextExtensions {
         public static HttpContext GetHttpContext(this ConnectionContext connection);
     }
     public class HttpConnectionDispatcherOptions {
         public HttpConnectionDispatcherOptions();
         public long ApplicationMaxBufferSize { get; set; }
         public IList<IAuthorizeData> AuthorizationData { get; }
         public LongPollingOptions LongPolling { get; }
         public long TransportMaxBufferSize { get; set; }
         public HttpTransportType Transports { get; set; }
         public WebSocketOptions WebSockets { get; }
     }
     public static class HttpTransports {
         public static readonly HttpTransportType All;
     }
     public enum HttpTransportType {
         LongPolling = 4,
         None = 0,
         ServerSentEvents = 2,
         WebSockets = 1,
     }
     public class LongPollingOptions {
         public LongPollingOptions();
         public TimeSpan PollTimeout { get; set; }
     }
+    public class NegotiateMetadata {
+        public NegotiateMetadata();
+    }
     public static class NegotiateProtocol {
         public static NegotiationResponse ParseResponse(Stream content);
+        public static NegotiationResponse ParseResponse(ReadOnlySpan<byte> content);
         public static void WriteResponse(NegotiationResponse response, IBufferWriter<byte> output);
     }
     public class NegotiationResponse {
         public NegotiationResponse();
         public string AccessToken { get; set; }
         public IList<AvailableTransport> AvailableTransports { get; set; }
         public string ConnectionId { get; set; }
         public string Error { get; set; }
         public string Url { get; set; }
     }
     public class WebSocketOptions {
         public WebSocketOptions();
         public TimeSpan CloseTimeout { get; set; }
         public Func<IList<string>, string> SubProtocolSelector { get; set; }
     }
 }
```


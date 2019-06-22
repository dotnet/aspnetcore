# Microsoft.AspNetCore.Http.Connections.Internal

``` diff
-namespace Microsoft.AspNetCore.Http.Connections.Internal {
 {
-    public class HttpConnectionContext : ConnectionContext, IConnectionHeartbeatFeature, IConnectionIdFeature, IConnectionInherentKeepAliveFeature, IConnectionItemsFeature, IConnectionTransportFeature, IConnectionUserFeature, IHttpContextFeature, IHttpTransportFeature, ITransferFormatFeature {
 {
-        public HttpConnectionContext(string id, ILogger logger);

-        public HttpConnectionContext(string id, IDuplexPipe transport, IDuplexPipe application, ILogger logger = null);

-        public TransferFormat ActiveFormat { get; set; }

-        public IDuplexPipe Application { get; set; }

-        public Task ApplicationTask { get; set; }

-        public CancellationTokenSource Cancellation { get; set; }

-        public override string ConnectionId { get; set; }

-        public override IFeatureCollection Features { get; }

-        public bool HasInherentKeepAlive { get; set; }

-        public HttpContext HttpContext { get; set; }

-        public override IDictionary<object, object> Items { get; set; }

-        public DateTime LastSeenUtc { get; set; }

-        public Task PreviousPollTask { get; set; }

-        public SemaphoreSlim StateLock { get; }

-        public HttpConnectionStatus Status { get; set; }

-        public TransferFormat SupportedFormats { get; set; }

-        public override IDuplexPipe Transport { get; set; }

-        public Task TransportTask { get; set; }

-        public HttpTransportType TransportType { get; set; }

-        public ClaimsPrincipal User { get; set; }

-        public SemaphoreSlim WriteLock { get; }

-        public Task DisposeAsync(bool closeGracefully = false);

-        public void OnHeartbeat(Action<object> action, object state);

-        public void TickHeartbeat();

-    }
-    public class HttpConnectionDispatcher {
 {
-        public HttpConnectionDispatcher(HttpConnectionManager manager, ILoggerFactory loggerFactory);

-        public Task ExecuteAsync(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionDelegate connectionDelegate);

-        public Task ExecuteNegotiateAsync(HttpContext context, HttpConnectionDispatcherOptions options);

-    }
-    public class HttpConnectionManager {
 {
-        public HttpConnectionManager(ILoggerFactory loggerFactory, IApplicationLifetime appLifetime);

-        public void CloseConnections();

-        public HttpConnectionContext CreateConnection();

-        public HttpConnectionContext CreateConnection(PipeOptions transportPipeOptions, PipeOptions appPipeOptions);

-        public Task DisposeAndRemoveAsync(HttpConnectionContext connection, bool closeGracefully);

-        public void RemoveConnection(string id);

-        public Task ScanAsync();

-        public void Start();

-        public bool TryGetConnection(string id, out HttpConnectionContext connection);

-    }
-    public enum HttpConnectionStatus {
 {
-        Active = 1,

-        Disposed = 2,

-        Inactive = 0,

-    }
-    public static class ServerSentEventsMessageFormatter {
 {
-        public static Task WriteMessageAsync(ReadOnlySequence<byte> payload, Stream output);

-    }
-}
```


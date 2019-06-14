# Microsoft.AspNetCore.Http.Connections.Internal

``` diff
 namespace Microsoft.AspNetCore.Http.Connections.Internal {
     public class HttpConnectionContext : ConnectionContext, IConnectionHeartbeatFeature, IConnectionIdFeature, IConnectionInherentKeepAliveFeature, IConnectionItemsFeature, IConnectionTransportFeature, IConnectionUserFeature, IHttpContextFeature, IHttpTransportFeature, ITransferFormatFeature {
+        public Nullable<DateTime> LastSeenUtcIfInactive { get; }
-        public SemaphoreSlim StateLock { get; }

+        public void MarkInactive();
+        public bool TryActivateLongPollingConnection(ConnectionDelegate connectionDelegate, HttpContext nonClonedContext, TimeSpan pollTimeout, Task currentRequestTask, ILoggerFactory loggerFactory, ILogger dispatcherLogger);
+        public bool TryActivatePersistentConnection(ConnectionDelegate connectionDelegate, IHttpTransport transport, ILogger dispatcherLogger);
     }
     public class HttpConnectionManager {
-        public HttpConnectionManager(ILoggerFactory loggerFactory, IApplicationLifetime appLifetime);

+        public HttpConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime);
+        public HttpConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IOptions<ConnectionOptions> connectionOptions);
+        public void Scan();
-        public Task ScanAsync();

     }
 }
```


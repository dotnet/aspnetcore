# Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets {
-    public sealed class SocketTransportFactory : ITransportFactory {
+    public sealed class SocketTransportFactory : IConnectionListenerFactory {
-        public SocketTransportFactory(IOptions<SocketTransportOptions> options, IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory);

+        public SocketTransportFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory);
+        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default(CancellationToken));
-        public ITransport Create(IEndPointInformation endPointInformation, IConnectionDispatcher dispatcher);

     }
     public class SocketTransportOptions {
         public SocketTransportOptions();
         public int IOQueueCount { get; set; }
+        public Nullable<long> MaxReadBufferSize { get; set; }
+        public Nullable<long> MaxWriteBufferSize { get; set; }
+        public bool NoDelay { get; set; }
     }
 }
```


# Microsoft.AspNetCore.Server.Kestrel.Core

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Core {
     public sealed class BadHttpRequestException : IOException {
-        public static void Throw(RequestRejectionReason reason, HttpMethod method);

     }
     public class KestrelServer : IDisposable, IServer {
+        public KestrelServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory);
-        public KestrelServer(IOptions<KestrelServerOptions> options, ITransportFactory transportFactory, ILoggerFactory loggerFactory);

     }
     public class KestrelServerOptions {
-        public SchedulingMode ApplicationSchedulingMode { get; set; }

+        public bool DisableStringReuse { get; set; }
     }
-    public class ListenOptions : IConnectionBuilder, IEndPointInformation {
+    public class ListenOptions : IConnectionBuilder {
+        public EndPoint EndPoint { get; }
-        public FileHandleType HandleType { get; set; }

-        public IPEndPoint IPEndPoint { get; set; }
+        public IPEndPoint IPEndPoint { get; }
-        public bool NoDelay { get; set; }

-        public ListenType Type { get; }

     }
-    public class ServerAddress {
 {
-        public ServerAddress();

-        public string Host { get; private set; }

-        public bool IsUnixPipe { get; }

-        public string PathBase { get; private set; }

-        public int Port { get; internal set; }

-        public string Scheme { get; private set; }

-        public string UnixPipePath { get; }

-        public override bool Equals(object obj);

-        public static ServerAddress FromUrl(string url);

-        public override int GetHashCode();

-        public override string ToString();

-    }
 }
```


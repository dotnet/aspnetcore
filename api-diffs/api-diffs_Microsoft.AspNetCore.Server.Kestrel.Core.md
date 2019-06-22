# Microsoft.AspNetCore.Server.Kestrel.Core

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Core {
     public sealed class BadHttpRequestException : IOException {
         public int StatusCode { get; }
-        public static void Throw(RequestRejectionReason reason, HttpMethod method);

     }
     public class Http2Limits {
         public Http2Limits();
         public int HeaderTableSize { get; set; }
         public int InitialConnectionWindowSize { get; set; }
         public int InitialStreamWindowSize { get; set; }
         public int MaxFrameSize { get; set; }
         public int MaxRequestHeaderFieldSize { get; set; }
         public int MaxStreamsPerConnection { get; set; }
     }
     public enum HttpProtocols {
         Http1 = 1,
         Http1AndHttp2 = 3,
         Http2 = 2,
         None = 0,
     }
     public class KestrelServer : IDisposable, IServer {
+        public KestrelServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory);
-        public KestrelServer(IOptions<KestrelServerOptions> options, ITransportFactory transportFactory, ILoggerFactory loggerFactory);

         public IFeatureCollection Features { get; }
         public KestrelServerOptions Options { get; }
         public void Dispose();
         public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken);
         public Task StopAsync(CancellationToken cancellationToken);
     }
     public class KestrelServerLimits {
         public KestrelServerLimits();
         public Http2Limits Http2 { get; }
         public TimeSpan KeepAliveTimeout { get; set; }
         public Nullable<long> MaxConcurrentConnections { get; set; }
         public Nullable<long> MaxConcurrentUpgradedConnections { get; set; }
         public Nullable<long> MaxRequestBodySize { get; set; }
         public Nullable<long> MaxRequestBufferSize { get; set; }
         public int MaxRequestHeaderCount { get; set; }
         public int MaxRequestHeadersTotalSize { get; set; }
         public int MaxRequestLineSize { get; set; }
         public Nullable<long> MaxResponseBufferSize { get; set; }
         public MinDataRate MinRequestBodyDataRate { get; set; }
         public MinDataRate MinResponseDataRate { get; set; }
         public TimeSpan RequestHeadersTimeout { get; set; }
     }
     public class KestrelServerOptions {
         public KestrelServerOptions();
         public bool AddServerHeader { get; set; }
         public bool AllowSynchronousIO { get; set; }
-        public SchedulingMode ApplicationSchedulingMode { get; set; }

         public IServiceProvider ApplicationServices { get; set; }
         public KestrelConfigurationLoader ConfigurationLoader { get; set; }
+        public bool DisableStringReuse { get; set; }
         public KestrelServerLimits Limits { get; }
         public KestrelConfigurationLoader Configure();
         public KestrelConfigurationLoader Configure(IConfiguration config);
         public void ConfigureEndpointDefaults(Action<ListenOptions> configureOptions);
         public void ConfigureHttpsDefaults(Action<HttpsConnectionAdapterOptions> configureOptions);
         public void Listen(IPAddress address, int port);
         public void Listen(IPAddress address, int port, Action<ListenOptions> configure);
         public void Listen(IPEndPoint endPoint);
         public void Listen(IPEndPoint endPoint, Action<ListenOptions> configure);
         public void ListenAnyIP(int port);
         public void ListenAnyIP(int port, Action<ListenOptions> configure);
         public void ListenHandle(ulong handle);
         public void ListenHandle(ulong handle, Action<ListenOptions> configure);
         public void ListenLocalhost(int port);
         public void ListenLocalhost(int port, Action<ListenOptions> configure);
         public void ListenUnixSocket(string socketPath);
         public void ListenUnixSocket(string socketPath, Action<ListenOptions> configure);
     }
-    public class ListenOptions : IConnectionBuilder, IEndPointInformation {
+    public class ListenOptions : IConnectionBuilder {
         public IServiceProvider ApplicationServices { get; }
-        public List<IConnectionAdapter> ConnectionAdapters { get; }

         public ulong FileHandle { get; }
-        public FileHandleType HandleType { get; set; }

-        public IPEndPoint IPEndPoint { get; set; }
+        public IPEndPoint IPEndPoint { get; }
         public KestrelServerOptions KestrelServerOptions { get; internal set; }
-        public bool NoDelay { get; set; }

         public HttpProtocols Protocols { get; set; }
         public string SocketPath { get; }
-        public ListenType Type { get; }

         public ConnectionDelegate Build();
         public override string ToString();
         public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);
     }
     public class MinDataRate {
         public MinDataRate(double bytesPerSecond, TimeSpan gracePeriod);
         public double BytesPerSecond { get; }
         public TimeSpan GracePeriod { get; }
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


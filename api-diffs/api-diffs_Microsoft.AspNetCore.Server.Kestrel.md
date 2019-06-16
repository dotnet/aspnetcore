# Microsoft.AspNetCore.Server.Kestrel

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel {
     public class EndpointConfiguration {
         public IConfigurationSection ConfigSection { get; }
         public HttpsConnectionAdapterOptions HttpsOptions { get; }
         public bool IsHttps { get; }
         public ListenOptions ListenOptions { get; }
     }
     public class KestrelConfigurationLoader {
         public IConfiguration Configuration { get; }
         public KestrelServerOptions Options { get; }
         public KestrelConfigurationLoader AnyIPEndpoint(int port);
         public KestrelConfigurationLoader AnyIPEndpoint(int port, Action<ListenOptions> configure);
         public KestrelConfigurationLoader Endpoint(IPAddress address, int port);
         public KestrelConfigurationLoader Endpoint(IPAddress address, int port, Action<ListenOptions> configure);
         public KestrelConfigurationLoader Endpoint(IPEndPoint endPoint);
         public KestrelConfigurationLoader Endpoint(IPEndPoint endPoint, Action<ListenOptions> configure);
         public KestrelConfigurationLoader Endpoint(string name, Action<EndpointConfiguration> configureOptions);
         public KestrelConfigurationLoader HandleEndpoint(ulong handle);
         public KestrelConfigurationLoader HandleEndpoint(ulong handle, Action<ListenOptions> configure);
         public void Load();
         public KestrelConfigurationLoader LocalhostEndpoint(int port);
         public KestrelConfigurationLoader LocalhostEndpoint(int port, Action<ListenOptions> configure);
         public KestrelConfigurationLoader UnixSocketEndpoint(string socketPath);
         public KestrelConfigurationLoader UnixSocketEndpoint(string socketPath, Action<ListenOptions> configure);
     }
 }
```


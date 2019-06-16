# Microsoft.AspNetCore.Hosting.Server

``` diff
 namespace Microsoft.AspNetCore.Hosting.Server {
     public interface IHttpApplication<TContext> {
         TContext CreateContext(IFeatureCollection contextFeatures);
         void DisposeContext(TContext context, Exception exception);
         Task ProcessRequestAsync(TContext context);
     }
     public interface IServer : IDisposable {
         IFeatureCollection Features { get; }
         Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken);
         Task StopAsync(CancellationToken cancellationToken);
     }
 }
```


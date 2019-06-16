# Microsoft.Extensions.Hosting.Internal

``` diff
 namespace Microsoft.Extensions.Hosting.Internal {
-    public class ApplicationLifetime : IApplicationLifetime {
+    public class ApplicationLifetime : IApplicationLifetime, IHostApplicationLifetime {
         public ApplicationLifetime(ILogger<ApplicationLifetime> logger);
         public CancellationToken ApplicationStarted { get; }
         public CancellationToken ApplicationStopped { get; }
         public CancellationToken ApplicationStopping { get; }
         public void NotifyStarted();
         public void NotifyStopped();
         public void StopApplication();
     }
     public class ConsoleLifetime : IDisposable, IHostLifetime {
+        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime);
+        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory);
-        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostingEnvironment environment, IApplicationLifetime applicationLifetime);

         public void Dispose();
         public Task StopAsync(CancellationToken cancellationToken);
         public Task WaitForStartAsync(CancellationToken cancellationToken);
     }
-    public class HostingEnvironment : IHostingEnvironment {
+    public class HostingEnvironment : IHostEnvironment, IHostingEnvironment {
         public HostingEnvironment();
         public string ApplicationName { get; set; }
         public IFileProvider ContentRootFileProvider { get; set; }
         public string ContentRootPath { get; set; }
         public string EnvironmentName { get; set; }
     }
 }
```


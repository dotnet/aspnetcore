# Microsoft.Extensions.Hosting.Internal

``` diff
 namespace Microsoft.Extensions.Hosting.Internal {
-    public class ApplicationLifetime : IApplicationLifetime
+    public class ApplicationLifetime : IApplicationLifetime, IHostApplicationLifetime
     public class ConsoleLifetime : IDisposable, IHostLifetime {
+        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime);
+        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory);
-        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostingEnvironment environment, IApplicationLifetime applicationLifetime);

     }
-    public class HostingEnvironment : IHostingEnvironment
+    public class HostingEnvironment : IHostEnvironment, IHostingEnvironment
 }
```


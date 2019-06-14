# Microsoft.AspNetCore.Hosting.Internal

``` diff
 namespace Microsoft.AspNetCore.Hosting.Internal {
-    public class ApplicationLifetime : IApplicationLifetime, IApplicationLifetime
+    public class ApplicationLifetime : IApplicationLifetime, IApplicationLifetime, IHostApplicationLifetime
-    public class AutoRequestServicesStartupFilter : IStartupFilter {
 {
-        public AutoRequestServicesStartupFilter();

-        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next);

-    }
+    public class ConventionBasedStartup : IStartup {
+        public ConventionBasedStartup(StartupMethods methods);
+        public void Configure(IApplicationBuilder app);
+        public IServiceProvider ConfigureServices(IServiceCollection services);
+    }
-    public class HostingEnvironment : IHostingEnvironment, IHostingEnvironment
+    public class HostingEnvironment : IHostEnvironment, IHostingEnvironment, IHostingEnvironment, IWebHostEnvironment
     public static class HostingEnvironmentExtensions {
+        public static void Initialize(this IWebHostEnvironment hostingEnvironment, string contentRootPath, WebHostOptions options);
     }
     public sealed class HostingEventSource : EventSource {
+        protected override void OnEventCommand(EventCommandEventArgs command);
     }
-    public class RequestServicesContainerMiddleware {
 {
-        public RequestServicesContainerMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory);

-        public Task Invoke(HttpContext httpContext);

-    }
-    public class RequestServicesFeature : IDisposable, IServiceProvidersFeature {
 {
-        public RequestServicesFeature(HttpContext context, IServiceScopeFactory scopeFactory);

-        public IServiceProvider RequestServices { get; set; }

-        public void Dispose();

-    }
 }
```


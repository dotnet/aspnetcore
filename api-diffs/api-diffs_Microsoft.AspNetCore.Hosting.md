# Microsoft.AspNetCore.Hosting

``` diff
 namespace Microsoft.AspNetCore.Hosting {
-    public class ConventionBasedStartup : IStartup {
 {
-        public ConventionBasedStartup(StartupMethods methods);

-        public void Configure(IApplicationBuilder app);

-        public IServiceProvider ConfigureServices(IServiceCollection services);

-    }
+    public interface IWebHostEnvironment : IHostEnvironment {
+        IFileProvider WebRootFileProvider { get; set; }
+        string WebRootPath { get; set; }
+    }
+    public static class StaticWebAssetsWebHostBuilderExtensions {
+        public static IWebHostBuilder UseStaticWebAssets(this IWebHostBuilder builder);
+    }
     public class WebHostBuilderContext {
-        public IHostingEnvironment HostingEnvironment { get; set; }
+        public IWebHostEnvironment HostingEnvironment { get; set; }
     }
     public static class WebHostBuilderExtensions {
+        public static IWebHostBuilder Configure(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, IApplicationBuilder> configureApp);
     }
 }
```


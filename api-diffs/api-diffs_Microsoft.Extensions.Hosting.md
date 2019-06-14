# Microsoft.Extensions.Hosting

``` diff
 namespace Microsoft.Extensions.Hosting {
+    public static class Environments {
+        public static readonly string Development;
+        public static readonly string Production;
+        public static readonly string Staging;
+    }
+    public static class GenericHostBuilderExtensions {
+        public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure);
+    }
+    public static class GenericHostWebHostBuilderExtensions {
+        public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure);
+    }
+    public static class Host {
+        public static IHostBuilder CreateDefaultBuilder();
+        public static IHostBuilder CreateDefaultBuilder(string[] args);
+    }
     public class HostBuilder : IHostBuilder {
+        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory);
     }
     public class HostBuilderContext {
-        public IHostingEnvironment HostingEnvironment { get; set; }
+        public IHostEnvironment HostingEnvironment { get; set; }
     }
+    public static class HostEnvironmentEnvExtensions {
+        public static bool IsDevelopment(this IHostEnvironment hostEnvironment);
+        public static bool IsEnvironment(this IHostEnvironment hostEnvironment, string environmentName);
+        public static bool IsProduction(this IHostEnvironment hostEnvironment);
+        public static bool IsStaging(this IHostEnvironment hostEnvironment);
+    }
     public static class HostingAbstractionsHostBuilderExtensions {
+        public static Task<IHost> StartAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default(CancellationToken));
     }
     public static class HostingHostBuilderExtensions {
+        public static Task RunConsoleAsync(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions, CancellationToken cancellationToken = default(CancellationToken));
+        public static IHostBuilder UseConsoleLifetime(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions);
+        public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<ServiceProviderOptions> configure);
+        public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<HostBuilderContext, ServiceProviderOptions> configure);
     }
+    public interface IHostApplicationLifetime {
+        CancellationToken ApplicationStarted { get; }
+        CancellationToken ApplicationStopped { get; }
+        CancellationToken ApplicationStopping { get; }
+        void StopApplication();
+    }
     public interface IHostBuilder {
+        IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory);
     }
+    public interface IHostEnvironment {
+        string ApplicationName { get; set; }
+        IFileProvider ContentRootFileProvider { get; set; }
+        string ContentRootPath { get; set; }
+        string EnvironmentName { get; set; }
+    }
 }
```


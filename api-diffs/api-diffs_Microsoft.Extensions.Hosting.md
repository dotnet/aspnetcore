# Microsoft.Extensions.Hosting

``` diff
 namespace Microsoft.Extensions.Hosting {
     public abstract class BackgroundService : IDisposable, IHostedService {
         protected BackgroundService();
         public virtual void Dispose();
         protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
         public virtual Task StartAsync(CancellationToken cancellationToken);
         public virtual Task StopAsync(CancellationToken cancellationToken);
     }
     public class ConsoleLifetimeOptions {
         public ConsoleLifetimeOptions();
         public bool SuppressStatusMessages { get; set; }
     }
     public static class EnvironmentName {
         public static readonly string Development;
         public static readonly string Production;
         public static readonly string Staging;
     }
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
         public HostBuilder();
         public IDictionary<object, object> Properties { get; }
         public IHost Build();
         public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate);
         public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate);
         public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate);
         public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
         public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory);
+        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory);
     }
     public class HostBuilderContext {
         public HostBuilderContext(IDictionary<object, object> properties);
         public IConfiguration Configuration { get; set; }
-        public IHostingEnvironment HostingEnvironment { get; set; }
+        public IHostEnvironment HostingEnvironment { get; set; }
         public IDictionary<object, object> Properties { get; }
     }
     public static class HostDefaults {
         public static readonly string ApplicationKey;
         public static readonly string ContentRootKey;
         public static readonly string EnvironmentKey;
     }
+    public static class HostEnvironmentEnvExtensions {
+        public static bool IsDevelopment(this IHostEnvironment hostEnvironment);
+        public static bool IsEnvironment(this IHostEnvironment hostEnvironment, string environmentName);
+        public static bool IsProduction(this IHostEnvironment hostEnvironment);
+        public static bool IsStaging(this IHostEnvironment hostEnvironment);
+    }
     public static class HostingAbstractionsHostBuilderExtensions {
         public static IHost Start(this IHostBuilder hostBuilder);
+        public static Task<IHost> StartAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default(CancellationToken));
     }
     public static class HostingAbstractionsHostExtensions {
         public static void Run(this IHost host);
         public static Task RunAsync(this IHost host, CancellationToken token = default(CancellationToken));
         public static void Start(this IHost host);
         public static Task StopAsync(this IHost host, TimeSpan timeout);
         public static void WaitForShutdown(this IHost host);
         public static Task WaitForShutdownAsync(this IHost host, CancellationToken token = default(CancellationToken));
     }
     public static class HostingEnvironmentExtensions {
         public static bool IsDevelopment(this IHostingEnvironment hostingEnvironment);
         public static bool IsEnvironment(this IHostingEnvironment hostingEnvironment, string environmentName);
         public static bool IsProduction(this IHostingEnvironment hostingEnvironment);
         public static bool IsStaging(this IHostingEnvironment hostingEnvironment);
     }
     public static class HostingHostBuilderExtensions {
         public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder hostBuilder, Action<IConfigurationBuilder> configureDelegate);
         public static IHostBuilder ConfigureContainer<TContainerBuilder>(this IHostBuilder hostBuilder, Action<TContainerBuilder> configureDelegate);
         public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<HostBuilderContext, ILoggingBuilder> configureLogging);
         public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging);
         public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder, Action<IServiceCollection> configureDelegate);
+        public static Task RunConsoleAsync(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions, CancellationToken cancellationToken = default(CancellationToken));
         public static Task RunConsoleAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default(CancellationToken));
         public static IHostBuilder UseConsoleLifetime(this IHostBuilder hostBuilder);
+        public static IHostBuilder UseConsoleLifetime(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions);
         public static IHostBuilder UseContentRoot(this IHostBuilder hostBuilder, string contentRoot);
+        public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<ServiceProviderOptions> configure);
+        public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<HostBuilderContext, ServiceProviderOptions> configure);
         public static IHostBuilder UseEnvironment(this IHostBuilder hostBuilder, string environment);
     }
     public class HostOptions {
         public HostOptions();
         public TimeSpan ShutdownTimeout { get; set; }
     }
     public interface IApplicationLifetime {
         CancellationToken ApplicationStarted { get; }
         CancellationToken ApplicationStopped { get; }
         CancellationToken ApplicationStopping { get; }
         void StopApplication();
     }
     public interface IHost : IDisposable {
         IServiceProvider Services { get; }
         Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
         Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
+    public interface IHostApplicationLifetime {
+        CancellationToken ApplicationStarted { get; }
+        CancellationToken ApplicationStopped { get; }
+        CancellationToken ApplicationStopping { get; }
+        void StopApplication();
+    }
     public interface IHostBuilder {
         IDictionary<object, object> Properties { get; }
         IHost Build();
         IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate);
         IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate);
         IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate);
         IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
         IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory);
+        IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory);
     }
     public interface IHostedService {
         Task StartAsync(CancellationToken cancellationToken);
         Task StopAsync(CancellationToken cancellationToken);
     }
+    public interface IHostEnvironment {
+        string ApplicationName { get; set; }
+        IFileProvider ContentRootFileProvider { get; set; }
+        string ContentRootPath { get; set; }
+        string EnvironmentName { get; set; }
+    }
     public interface IHostingEnvironment {
         string ApplicationName { get; set; }
         IFileProvider ContentRootFileProvider { get; set; }
         string ContentRootPath { get; set; }
         string EnvironmentName { get; set; }
     }
     public interface IHostLifetime {
         Task StopAsync(CancellationToken cancellationToken);
         Task WaitForStartAsync(CancellationToken cancellationToken);
     }
 }
```


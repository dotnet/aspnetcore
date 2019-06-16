# Microsoft.AspNetCore.Hosting

``` diff
 namespace Microsoft.AspNetCore.Hosting {
-    public class ConventionBasedStartup : IStartup {
 {
-        public ConventionBasedStartup(StartupMethods methods);

-        public void Configure(IApplicationBuilder app);

-        public IServiceProvider ConfigureServices(IServiceCollection services);

-    }
     public class DelegateStartup : StartupBase<IServiceCollection> {
         public DelegateStartup(IServiceProviderFactory<IServiceCollection> factory, Action<IApplicationBuilder> configureApp);
         public override void Configure(IApplicationBuilder app);
     }
     public static class EnvironmentName {
         public static readonly string Development;
         public static readonly string Production;
         public static readonly string Staging;
     }
     public static class HostingAbstractionsWebHostBuilderExtensions {
         public static IWebHostBuilder CaptureStartupErrors(this IWebHostBuilder hostBuilder, bool captureStartupErrors);
         public static IWebHostBuilder PreferHostingUrls(this IWebHostBuilder hostBuilder, bool preferHostingUrls);
         public static IWebHost Start(this IWebHostBuilder hostBuilder, params string[] urls);
         public static IWebHostBuilder SuppressStatusMessages(this IWebHostBuilder hostBuilder, bool suppressStatusMessages);
         public static IWebHostBuilder UseConfiguration(this IWebHostBuilder hostBuilder, IConfiguration configuration);
         public static IWebHostBuilder UseContentRoot(this IWebHostBuilder hostBuilder, string contentRoot);
         public static IWebHostBuilder UseEnvironment(this IWebHostBuilder hostBuilder, string environment);
         public static IWebHostBuilder UseServer(this IWebHostBuilder hostBuilder, IServer server);
         public static IWebHostBuilder UseShutdownTimeout(this IWebHostBuilder hostBuilder, TimeSpan timeout);
         public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, string startupAssemblyName);
         public static IWebHostBuilder UseUrls(this IWebHostBuilder hostBuilder, params string[] urls);
         public static IWebHostBuilder UseWebRoot(this IWebHostBuilder hostBuilder, string webRoot);
     }
     public static class HostingEnvironmentExtensions {
         public static bool IsDevelopment(this IHostingEnvironment hostingEnvironment);
         public static bool IsEnvironment(this IHostingEnvironment hostingEnvironment, string environmentName);
         public static bool IsProduction(this IHostingEnvironment hostingEnvironment);
         public static bool IsStaging(this IHostingEnvironment hostingEnvironment);
     }
     public sealed class HostingStartupAttribute : Attribute {
         public HostingStartupAttribute(Type hostingStartupType);
         public Type HostingStartupType { get; }
     }
     public interface IApplicationLifetime {
         CancellationToken ApplicationStarted { get; }
         CancellationToken ApplicationStopped { get; }
         CancellationToken ApplicationStopping { get; }
         void StopApplication();
     }
     public interface IHostingEnvironment {
         string ApplicationName { get; set; }
         IFileProvider ContentRootFileProvider { get; set; }
         string ContentRootPath { get; set; }
         string EnvironmentName { get; set; }
         IFileProvider WebRootFileProvider { get; set; }
         string WebRootPath { get; set; }
     }
     public interface IHostingStartup {
         void Configure(IWebHostBuilder builder);
     }
     public interface IStartup {
         void Configure(IApplicationBuilder app);
         IServiceProvider ConfigureServices(IServiceCollection services);
     }
     public interface IStartupFilter {
         Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next);
     }
     public interface IWebHost : IDisposable {
         IFeatureCollection ServerFeatures { get; }
         IServiceProvider Services { get; }
         void Start();
         Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
         Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public interface IWebHostBuilder {
         IWebHost Build();
         IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);
         IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices);
         IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);
         string GetSetting(string key);
         IWebHostBuilder UseSetting(string key, string value);
     }
+    public interface IWebHostEnvironment : IHostEnvironment {
+        IFileProvider WebRootFileProvider { get; set; }
+        string WebRootPath { get; set; }
+    }
     public static class KestrelServerOptionsSystemdExtensions {
         public static KestrelServerOptions UseSystemd(this KestrelServerOptions options);
         public static KestrelServerOptions UseSystemd(this KestrelServerOptions options, Action<ListenOptions> configure);
     }
     public static class ListenOptionsConnectionLoggingExtensions {
         public static ListenOptions UseConnectionLogging(this ListenOptions listenOptions);
         public static ListenOptions UseConnectionLogging(this ListenOptions listenOptions, string loggerName);
     }
     public static class ListenOptionsHttpsExtensions {
         public static ListenOptions UseHttps(this ListenOptions listenOptions);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, Action<HttpsConnectionAdapterOptions> configureOptions);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid, StoreLocation location);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, StoreName storeName, string subject, bool allowInvalid, StoreLocation location, Action<HttpsConnectionAdapterOptions> configureOptions);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, X509Certificate2 serverCertificate);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, X509Certificate2 serverCertificate, Action<HttpsConnectionAdapterOptions> configureOptions);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName, string password);
         public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName, string password, Action<HttpsConnectionAdapterOptions> configureOptions);
     }
     public abstract class StartupBase : IStartup {
         protected StartupBase();
         public abstract void Configure(IApplicationBuilder app);
         public virtual void ConfigureServices(IServiceCollection services);
         public virtual IServiceProvider CreateServiceProvider(IServiceCollection services);
         IServiceProvider Microsoft.AspNetCore.Hosting.IStartup.ConfigureServices(IServiceCollection services);
     }
     public abstract class StartupBase<TBuilder> : StartupBase {
         public StartupBase(IServiceProviderFactory<TBuilder> factory);
         public virtual void ConfigureContainer(TBuilder builder);
         public override IServiceProvider CreateServiceProvider(IServiceCollection services);
     }
+    public static class StaticWebAssetsWebHostBuilderExtensions {
+        public static IWebHostBuilder UseStaticWebAssets(this IWebHostBuilder builder);
+    }
     public class WebHostBuilder : IWebHostBuilder {
         public WebHostBuilder();
         public IWebHost Build();
         public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);
         public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices);
         public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);
         public string GetSetting(string key);
         public IWebHostBuilder UseSetting(string key, string value);
     }
     public class WebHostBuilderContext {
         public WebHostBuilderContext();
         public IConfiguration Configuration { get; set; }
-        public IHostingEnvironment HostingEnvironment { get; set; }
+        public IWebHostEnvironment HostingEnvironment { get; set; }
     }
     public static class WebHostBuilderExtensions {
         public static IWebHostBuilder Configure(this IWebHostBuilder hostBuilder, Action<IApplicationBuilder> configureApp);
+        public static IWebHostBuilder Configure(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, IApplicationBuilder> configureApp);
         public static IWebHostBuilder ConfigureAppConfiguration(this IWebHostBuilder hostBuilder, Action<IConfigurationBuilder> configureDelegate);
         public static IWebHostBuilder ConfigureLogging(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, ILoggingBuilder> configureLogging);
         public static IWebHostBuilder ConfigureLogging(this IWebHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging);
         public static IWebHostBuilder UseDefaultServiceProvider(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, ServiceProviderOptions> configure);
         public static IWebHostBuilder UseDefaultServiceProvider(this IWebHostBuilder hostBuilder, Action<ServiceProviderOptions> configure);
         public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, Type startupType);
         public static IWebHostBuilder UseStartup<TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class;
     }
     public static class WebHostBuilderHttpSysExtensions {
         public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder);
         public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder, Action<HttpSysOptions> options);
     }
     public static class WebHostBuilderIISExtensions {
         public static IWebHostBuilder UseIIS(this IWebHostBuilder hostBuilder);
         public static IWebHostBuilder UseIISIntegration(this IWebHostBuilder hostBuilder);
     }
     public static class WebHostBuilderKestrelExtensions {
         public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions);
         public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options);
         public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder);
         public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions);
         public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options);
     }
     public static class WebHostBuilderSocketExtensions {
         public static IWebHostBuilder UseSockets(this IWebHostBuilder hostBuilder);
         public static IWebHostBuilder UseSockets(this IWebHostBuilder hostBuilder, Action<SocketTransportOptions> configureOptions);
     }
     public static class WebHostDefaults {
         public static readonly string ApplicationKey;
         public static readonly string CaptureStartupErrorsKey;
         public static readonly string ContentRootKey;
         public static readonly string DetailedErrorsKey;
         public static readonly string EnvironmentKey;
         public static readonly string HostingStartupAssembliesKey;
         public static readonly string HostingStartupExcludeAssembliesKey;
         public static readonly string PreferHostingUrlsKey;
         public static readonly string PreventHostingStartupKey;
         public static readonly string ServerUrlsKey;
         public static readonly string ShutdownTimeoutKey;
         public static readonly string StartupAssemblyKey;
         public static readonly string SuppressStatusMessagesKey;
         public static readonly string WebRootKey;
     }
     public static class WebHostExtensions {
         public static void Run(this IWebHost host);
         public static Task RunAsync(this IWebHost host, CancellationToken token = default(CancellationToken));
         public static Task StopAsync(this IWebHost host, TimeSpan timeout);
         public static void WaitForShutdown(this IWebHost host);
         public static Task WaitForShutdownAsync(this IWebHost host, CancellationToken token = default(CancellationToken));
     }
 }
```


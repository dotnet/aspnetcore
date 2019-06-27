# Microsoft.AspNetCore.Hosting.Internal

``` diff
-namespace Microsoft.AspNetCore.Hosting.Internal {
 {
-    public class ApplicationLifetime : IApplicationLifetime, IApplicationLifetime {
 {
-        public ApplicationLifetime(ILogger<ApplicationLifetime> logger);

-        public CancellationToken ApplicationStarted { get; }

-        public CancellationToken ApplicationStopped { get; }

-        public CancellationToken ApplicationStopping { get; }

-        public void NotifyStarted();

-        public void NotifyStopped();

-        public void StopApplication();

-    }
-    public class AutoRequestServicesStartupFilter : IStartupFilter {
 {
-        public AutoRequestServicesStartupFilter();

-        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next);

-    }
-    public class ConfigureBuilder {
 {
-        public ConfigureBuilder(MethodInfo configure);

-        public MethodInfo MethodInfo { get; }

-        public Action<IApplicationBuilder> Build(object instance);

-    }
-    public class ConfigureContainerBuilder {
 {
-        public ConfigureContainerBuilder(MethodInfo configureContainerMethod);

-        public Func<Action<object>, Action<object>> ConfigureContainerFilters { get; set; }

-        public MethodInfo MethodInfo { get; }

-        public Action<object> Build(object instance);

-        public Type GetContainerType();

-    }
-    public class ConfigureServicesBuilder {
 {
-        public ConfigureServicesBuilder(MethodInfo configureServices);

-        public MethodInfo MethodInfo { get; }

-        public Func<Func<IServiceCollection, IServiceProvider>, Func<IServiceCollection, IServiceProvider>> StartupServiceFilters { get; set; }

-        public Func<IServiceCollection, IServiceProvider> Build(object instance);

-    }
-    public class HostedServiceExecutor {
 {
-        public HostedServiceExecutor(ILogger<HostedServiceExecutor> logger, IEnumerable<IHostedService> services);

-        public Task StartAsync(CancellationToken token);

-        public Task StopAsync(CancellationToken token);

-    }
-    public class HostingApplication : IHttpApplication<HostingApplication.Context> {
 {
-        public HostingApplication(RequestDelegate application, ILogger logger, DiagnosticListener diagnosticSource, IHttpContextFactory httpContextFactory);

-        public HostingApplication.Context CreateContext(IFeatureCollection contextFeatures);

-        public void DisposeContext(HostingApplication.Context context, Exception exception);

-        public Task ProcessRequestAsync(HostingApplication.Context context);

-        public struct Context {
 {
-            public Activity Activity { get; set; }

-            public bool EventLogEnabled { get; set; }

-            public HttpContext HttpContext { get; set; }

-            public IDisposable Scope { get; set; }

-            public long StartTimestamp { get; set; }

-        }
-    }
-    public class HostingEnvironment : IHostingEnvironment, IHostingEnvironment {
 {
-        public HostingEnvironment();

-        public string ApplicationName { get; set; }

-        public IFileProvider ContentRootFileProvider { get; set; }

-        public string ContentRootPath { get; set; }

-        public string EnvironmentName { get; set; }

-        public IFileProvider WebRootFileProvider { get; set; }

-        public string WebRootPath { get; set; }

-    }
-    public static class HostingEnvironmentExtensions {
 {
-        public static void Initialize(this IHostingEnvironment hostingEnvironment, string contentRootPath, WebHostOptions options);

-    }
-    public sealed class HostingEventSource : EventSource {
 {
-        public static readonly HostingEventSource Log;

-        public void HostStart();

-        public void HostStop();

-        public void RequestStart(string method, string path);

-        public void RequestStop();

-        public void UnhandledException();

-    }
-    public interface IStartupConfigureContainerFilter<TContainerBuilder> {
 {
-        Action<TContainerBuilder> ConfigureContainer(Action<TContainerBuilder> container);

-    }
-    public interface IStartupConfigureServicesFilter {
 {
-        Action<IServiceCollection> ConfigureServices(Action<IServiceCollection> next);

-    }
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
-    public class StartupLoader {
 {
-        public StartupLoader();

-        public static Type FindStartupType(string startupAssemblyName, string environmentName);

-        public static StartupMethods LoadMethods(IServiceProvider hostingServiceProvider, Type startupType, string environmentName);

-    }
-    public class StartupMethods {
 {
-        public StartupMethods(object instance, Action<IApplicationBuilder> configure, Func<IServiceCollection, IServiceProvider> configureServices);

-        public Action<IApplicationBuilder> ConfigureDelegate { get; }

-        public Func<IServiceCollection, IServiceProvider> ConfigureServicesDelegate { get; }

-        public object StartupInstance { get; }

-    }
-    public class WebHostOptions {
 {
-        public WebHostOptions();

-        public WebHostOptions(IConfiguration configuration);

-        public WebHostOptions(IConfiguration configuration, string applicationNameFallback);

-        public string ApplicationName { get; set; }

-        public bool CaptureStartupErrors { get; set; }

-        public string ContentRootPath { get; set; }

-        public bool DetailedErrors { get; set; }

-        public string Environment { get; set; }

-        public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

-        public IReadOnlyList<string> HostingStartupExcludeAssemblies { get; set; }

-        public bool PreventHostingStartup { get; set; }

-        public TimeSpan ShutdownTimeout { get; set; }

-        public string StartupAssembly { get; set; }

-        public bool SuppressStatusMessages { get; set; }

-        public string WebRoot { get; set; }

-        public IEnumerable<string> GetFinalHostingStartupAssemblies();

-    }
-    public class WebHostUtilities {
 {
-        public WebHostUtilities();

-        public static bool ParseBool(IConfiguration configuration, string key);

-    }
-}
```


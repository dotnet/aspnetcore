// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    internal partial class ConfigureBuilder
    {
        public ConfigureBuilder(System.Reflection.MethodInfo configure) { }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Build(object instance) { throw null; }
    }
    internal partial class ConfigureContainerBuilder
    {
        public ConfigureContainerBuilder(System.Reflection.MethodInfo configureContainerMethod) { }
        public System.Func<System.Action<object>, System.Action<object>> ConfigureContainerFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<object> Build(object instance) { throw null; }
        public System.Type GetContainerType() { throw null; }
    }
    internal partial class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(System.Reflection.MethodInfo configureServices) { }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider>, System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider>> StartupServiceFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> Build(object instance) { throw null; }
    }
    internal partial class GenericWebHostBuilder : Microsoft.AspNetCore.Hosting.ISupportsStartup, Microsoft.AspNetCore.Hosting.ISupportsUseDefaultServiceProvider, Microsoft.AspNetCore.Hosting.IWebHostBuilder
    {
        public GenericWebHostBuilder(Microsoft.Extensions.Hosting.IHostBuilder builder) { }
        public Microsoft.AspNetCore.Hosting.IWebHost Build() { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder Configure(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.AspNetCore.Builder.IApplicationBuilder> configure) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureAppConfiguration(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureServices(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceCollection> configureServices) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureServices(System.Action<Microsoft.Extensions.DependencyInjection.IServiceCollection> configureServices) { throw null; }
        public string GetSetting(string key) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder UseDefaultServiceProvider(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSetting(string key, string value) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder UseStartup(System.Type startupType) { throw null; }
    }
    internal partial class GenericWebHostService : Microsoft.Extensions.Hosting.IHostedService
    {
        public GenericWebHostService(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Hosting.GenericWebHostServiceOptions> options, Microsoft.AspNetCore.Hosting.Server.IServer server, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Http.IHttpContextFactory httpContextFactory, Microsoft.AspNetCore.Hosting.Builder.IApplicationBuilderFactory applicationBuilderFactory, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Hosting.IStartupFilter> startupFilters, Microsoft.Extensions.Configuration.IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment) { }
        public Microsoft.AspNetCore.Hosting.Builder.IApplicationBuilderFactory ApplicationBuilderFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Diagnostics.DiagnosticListener DiagnosticListener { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Hosting.IWebHostEnvironment HostingEnvironment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.IHttpContextFactory HttpContextFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Logging.ILogger LifetimeLogger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Hosting.GenericWebHostServiceOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Hosting.Server.IServer Server { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Hosting.IStartupFilter> StartupFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    internal partial class GenericWebHostServiceOptions
    {
        public GenericWebHostServiceOptions() { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> ConfigureApplication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.AggregateException HostingStartupExceptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Hosting.WebHostOptions WebHostOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class HostingApplication : Microsoft.AspNetCore.Hosting.Server.IHttpApplication<Microsoft.AspNetCore.Hosting.HostingApplication.Context>
    {
        public HostingApplication(Microsoft.AspNetCore.Http.RequestDelegate application, Microsoft.Extensions.Logging.ILogger logger, System.Diagnostics.DiagnosticListener diagnosticSource, Microsoft.AspNetCore.Http.IHttpContextFactory httpContextFactory) { }
        public Microsoft.AspNetCore.Hosting.HostingApplication.Context CreateContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection contextFeatures) { throw null; }
        public void DisposeContext(Microsoft.AspNetCore.Hosting.HostingApplication.Context context, System.Exception exception) { }
        public System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Hosting.HostingApplication.Context context) { throw null; }
        internal partial class Context
        {
            public Context() { }
            public System.Diagnostics.Activity Activity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool EventLogEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            internal bool HasDiagnosticListener { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.IDisposable Scope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public long StartTimestamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public void Reset() { }
        }
    }
    internal partial class HostingEnvironment : Microsoft.AspNetCore.Hosting.IHostingEnvironment, Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.Extensions.Hosting.IHostEnvironment, Microsoft.Extensions.Hosting.IHostingEnvironment
    {
        public HostingEnvironment() { }
        public string ApplicationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContentRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EnvironmentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string WebRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal static partial class HostingEnvironmentExtensions
    {
        internal static void Initialize(this Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment, string contentRootPath, Microsoft.AspNetCore.Hosting.WebHostOptions options) { }
        internal static void Initialize(this Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, string contentRootPath, Microsoft.AspNetCore.Hosting.WebHostOptions options) { }
    }
    internal sealed partial class HostingEventSource : System.Diagnostics.Tracing.EventSource
    {
        public static readonly Microsoft.AspNetCore.Hosting.HostingEventSource Log;
        internal HostingEventSource() { }
        internal HostingEventSource(string eventSourceName) { }
        [System.Diagnostics.Tracing.EventAttribute(1, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void HostStart() { }
        [System.Diagnostics.Tracing.EventAttribute(2, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void HostStop() { }
        protected override void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command) { }
        internal void RequestFailed() { }
        [System.Diagnostics.Tracing.EventAttribute(3, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void RequestStart(string method, string path) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(4, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void RequestStop() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(5, Level=System.Diagnostics.Tracing.EventLevel.Error)]
        public void UnhandledException() { }
    }
    internal partial class HostingRequestStartingLog : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        internal static readonly System.Func<object, System.Exception, string> Callback;
        public HostingRequestStartingLog(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override string ToString() { throw null; }
    }
    internal partial interface ISupportsStartup
    {
        Microsoft.AspNetCore.Hosting.IWebHostBuilder Configure(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.AspNetCore.Builder.IApplicationBuilder> configure);
        Microsoft.AspNetCore.Hosting.IWebHostBuilder UseStartup(System.Type startupType);
    }
    internal partial interface ISupportsUseDefaultServiceProvider
    {
        Microsoft.AspNetCore.Hosting.IWebHostBuilder UseDefaultServiceProvider(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure);
    }
    internal static partial class LoggerEventIds
    {
        public const int ApplicationStartupException = 6;
        public const int ApplicationStoppedException = 8;
        public const int ApplicationStoppingException = 7;
        public const int HostedServiceStartException = 9;
        public const int HostedServiceStopException = 10;
        public const int HostingStartupAssemblyException = 11;
        public const int RequestFinished = 2;
        public const int RequestStarting = 1;
        public const int ServerShutdownException = 12;
        public const int Shutdown = 5;
        public const int Started = 4;
        public const int Starting = 3;
    }
    internal partial class StartupLoader
    {
        public StartupLoader() { }
        internal static Microsoft.AspNetCore.Hosting.ConfigureContainerBuilder FindConfigureContainerDelegate(System.Type startupType, string environmentName) { throw null; }
        internal static Microsoft.AspNetCore.Hosting.ConfigureBuilder FindConfigureDelegate(System.Type startupType, string environmentName) { throw null; }
        internal static Microsoft.AspNetCore.Hosting.ConfigureServicesBuilder FindConfigureServicesDelegate(System.Type startupType, string environmentName) { throw null; }
        public static System.Type FindStartupType(string startupAssemblyName, string environmentName) { throw null; }
        internal static bool HasConfigureServicesIServiceProviderDelegate(System.Type startupType, string environmentName) { throw null; }
        public static Microsoft.AspNetCore.Hosting.StartupMethods LoadMethods(System.IServiceProvider hostingServiceProvider, System.Type startupType, string environmentName) { throw null; }
    }
    internal partial class StartupMethods
    {
        public StartupMethods(object instance, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configure, System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> configureServices) { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> ConfigureDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> ConfigureServicesDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object StartupInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class WebHostOptions
    {
        public WebHostOptions() { }
        public WebHostOptions(Microsoft.Extensions.Configuration.IConfiguration configuration) { }
        public WebHostOptions(Microsoft.Extensions.Configuration.IConfiguration configuration, string applicationNameFallback) { }
        public string ApplicationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool CaptureStartupErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContentRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool DetailedErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Environment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IReadOnlyList<string> HostingStartupAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IReadOnlyList<string> HostingStartupExcludeAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PreventHostingStartup { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan ShutdownTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string StartupAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SuppressStatusMessages { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string WebRoot { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<string> GetFinalHostingStartupAssemblies() { throw null; }
    }
}

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    internal partial class StaticWebAssetsFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        public StaticWebAssetsFileProvider(string pathPrefix, string contentRoot) { }
        public Microsoft.AspNetCore.Http.PathString BasePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.FileProviders.PhysicalFileProvider InnerProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter) { throw null; }
    }
    public partial class StaticWebAssetsLoader
    {
        internal const string StaticWebAssetsManifestName = "Microsoft.AspNetCore.StaticWebAssets.xml";
        internal static string GetAssemblyLocation(System.Reflection.Assembly assembly) { throw null; }
        internal static System.IO.Stream ResolveManifest(Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        internal static void UseStaticWebAssetsCore(Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment, System.IO.Stream manifest) { }
    }
    internal static partial class StaticWebAssetsReader
    {
        internal static System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Hosting.StaticWebAssets.StaticWebAssetsReader.ContentRootMapping> Parse(System.IO.Stream manifest) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal readonly partial struct ContentRootMapping
        {
            private readonly object _dummy;
            public ContentRootMapping(string basePath, string path) { throw null; }
            public string BasePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
    }
}

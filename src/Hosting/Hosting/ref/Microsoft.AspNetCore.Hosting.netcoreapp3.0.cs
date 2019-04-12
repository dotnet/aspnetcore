// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public partial class DelegateStartup : Microsoft.AspNetCore.Hosting.StartupBase<Microsoft.Extensions.DependencyInjection.IServiceCollection>
    {
        public DelegateStartup(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<Microsoft.Extensions.DependencyInjection.IServiceCollection> factory, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configureApp) : base (default(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<Microsoft.Extensions.DependencyInjection.IServiceCollection>)) { }
        public override void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app) { }
    }
    public abstract partial class StartupBase : Microsoft.AspNetCore.Hosting.IStartup
    {
        protected StartupBase() { }
        public abstract void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app);
        public virtual void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
        public virtual System.IServiceProvider CreateServiceProvider(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        System.IServiceProvider Microsoft.AspNetCore.Hosting.IStartup.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
    public abstract partial class StartupBase<TBuilder> : Microsoft.AspNetCore.Hosting.StartupBase
    {
        public StartupBase(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TBuilder> factory) { }
        public virtual void ConfigureContainer(TBuilder builder) { }
        public override System.IServiceProvider CreateServiceProvider(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
    public partial class WebHostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
    {
        public WebHostBuilder() { }
        public Microsoft.AspNetCore.Hosting.IWebHost Build() { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureAppConfiguration(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureServices(System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.DependencyInjection.IServiceCollection> configureServices) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureServices(System.Action<Microsoft.Extensions.DependencyInjection.IServiceCollection> configureServices) { throw null; }
        public string GetSetting(string key) { throw null; }
        public Microsoft.AspNetCore.Hosting.IWebHostBuilder UseSetting(string key, string value) { throw null; }
    }
    public static partial class WebHostBuilderExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder Configure(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configureApp) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder Configure(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.AspNetCore.Builder.IApplicationBuilder> configureApp) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureAppConfiguration(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> configureDelegate) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureLogging(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder ConfigureLogging(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.Extensions.Logging.ILoggingBuilder> configureLogging) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseDefaultServiceProvider(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Hosting.WebHostBuilderContext, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseDefaultServiceProvider(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.Extensions.DependencyInjection.ServiceProviderOptions> configure) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseStartup(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Type startupType) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseStartup<TStartup>(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) where TStartup : class { throw null; }
    }
    public static partial class WebHostExtensions
    {
        public static void Run(this Microsoft.AspNetCore.Hosting.IWebHost host) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task RunAsync(this Microsoft.AspNetCore.Hosting.IWebHost host, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task StopAsync(this Microsoft.AspNetCore.Hosting.IWebHost host, System.TimeSpan timeout) { throw null; }
        public static void WaitForShutdown(this Microsoft.AspNetCore.Hosting.IWebHost host) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task WaitForShutdownAsync(this Microsoft.AspNetCore.Hosting.IWebHost host, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Hosting.Builder
{
    public partial class ApplicationBuilderFactory : Microsoft.AspNetCore.Hosting.Builder.IApplicationBuilderFactory
    {
        public ApplicationBuilderFactory(System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder CreateBuilder(Microsoft.AspNetCore.Http.Features.IFeatureCollection serverFeatures) { throw null; }
    }
    public partial interface IApplicationBuilderFactory
    {
        Microsoft.AspNetCore.Builder.IApplicationBuilder CreateBuilder(Microsoft.AspNetCore.Http.Features.IFeatureCollection serverFeatures);
    }
}
namespace Microsoft.AspNetCore.Hosting.Internal
{
    public partial class ApplicationLifetime : Microsoft.AspNetCore.Hosting.IApplicationLifetime, Microsoft.Extensions.Hosting.IApplicationLifetime, Microsoft.Extensions.Hosting.IHostApplicationLifetime
    {
        public ApplicationLifetime(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Hosting.Internal.ApplicationLifetime> logger) { }
        public System.Threading.CancellationToken ApplicationStarted { get { throw null; } }
        public System.Threading.CancellationToken ApplicationStopped { get { throw null; } }
        public System.Threading.CancellationToken ApplicationStopping { get { throw null; } }
        public void NotifyStarted() { }
        public void NotifyStopped() { }
        public void StopApplication() { }
    }
    public partial class ConfigureBuilder
    {
        public ConfigureBuilder(System.Reflection.MethodInfo configure) { }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Build(object instance) { throw null; }
    }
    public partial class ConfigureContainerBuilder
    {
        public ConfigureContainerBuilder(System.Reflection.MethodInfo configureContainerMethod) { }
        public System.Func<System.Action<object>, System.Action<object>> ConfigureContainerFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<object> Build(object instance) { throw null; }
        public System.Type GetContainerType() { throw null; }
    }
    public partial class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder(System.Reflection.MethodInfo configureServices) { }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider>, System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider>> StartupServiceFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> Build(object instance) { throw null; }
    }
    public partial class ConventionBasedStartup : Microsoft.AspNetCore.Hosting.IStartup
    {
        public ConventionBasedStartup(Microsoft.AspNetCore.Hosting.Internal.StartupMethods methods) { }
        public void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app) { }
        public System.IServiceProvider ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
    public partial class HostedServiceExecutor
    {
        public HostedServiceExecutor(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Hosting.Internal.HostedServiceExecutor> logger, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Hosting.IHostedService> services) { }
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken token) { throw null; }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken token) { throw null; }
    }
    public partial class HostingApplication : Microsoft.AspNetCore.Hosting.Server.IHttpApplication<Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context>
    {
        public HostingApplication(Microsoft.AspNetCore.Http.RequestDelegate application, Microsoft.Extensions.Logging.ILogger logger, System.Diagnostics.DiagnosticListener diagnosticSource, Microsoft.AspNetCore.Http.IHttpContextFactory httpContextFactory) { }
        public Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context CreateContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection contextFeatures) { throw null; }
        public void DisposeContext(Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context context, System.Exception exception) { }
        public System.Threading.Tasks.Task ProcessRequestAsync(Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context context) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Context
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Diagnostics.Activity Activity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public bool EventLogEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public System.IDisposable Scope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public long StartTimestamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class HostingEnvironment : Microsoft.AspNetCore.Hosting.IHostingEnvironment, Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.Extensions.Hosting.IHostEnvironment, Microsoft.Extensions.Hosting.IHostingEnvironment
    {
        public HostingEnvironment() { }
        public string ApplicationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ContentRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EnvironmentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string WebRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class HostingEnvironmentExtensions
    {
        public static void Initialize(this Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment, string contentRootPath, Microsoft.AspNetCore.Hosting.Internal.WebHostOptions options) { }
        public static void Initialize(this Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, string contentRootPath, Microsoft.AspNetCore.Hosting.Internal.WebHostOptions options) { }
    }
    [System.Diagnostics.Tracing.EventSourceAttribute(Name="Microsoft-AspNetCore-Hosting")]
    public sealed partial class HostingEventSource : System.Diagnostics.Tracing.EventSource
    {
        internal HostingEventSource() { }
        public static readonly Microsoft.AspNetCore.Hosting.Internal.HostingEventSource Log;
        [System.Diagnostics.Tracing.EventAttribute(1, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void HostStart() { }
        [System.Diagnostics.Tracing.EventAttribute(2, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void HostStop() { }
        [System.Diagnostics.Tracing.EventAttribute(3, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void RequestStart(string method, string path) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(4, Level=System.Diagnostics.Tracing.EventLevel.Informational)]
        public void RequestStop() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(5, Level=System.Diagnostics.Tracing.EventLevel.Error)]
        public void UnhandledException() { }
    }
    public partial class StartupLoader
    {
        public StartupLoader() { }
        public static System.Type FindStartupType(string startupAssemblyName, string environmentName) { throw null; }
        public static Microsoft.AspNetCore.Hosting.Internal.StartupMethods LoadMethods(System.IServiceProvider hostingServiceProvider, System.Type startupType, string environmentName) { throw null; }
    }
    public partial class StartupMethods
    {
        public StartupMethods(object instance, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> configure, System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> configureServices) { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> ConfigureDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.Extensions.DependencyInjection.IServiceCollection, System.IServiceProvider> ConfigureServicesDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object StartupInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class WebHostOptions
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
    public partial class WebHostUtilities
    {
        public WebHostUtilities() { }
        public static bool ParseBool(Microsoft.Extensions.Configuration.IConfiguration configuration, string key) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Hosting.Server.Features
{
    public partial class ServerAddressesFeature : Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature
    {
        public ServerAddressesFeature() { }
        public System.Collections.Generic.ICollection<string> Addresses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool PreferHostingUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.Hosting
{
    public static partial class GenericHostWebHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureWebHost(this Microsoft.Extensions.Hosting.IHostBuilder builder, System.Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> configure) { throw null; }
    }
}

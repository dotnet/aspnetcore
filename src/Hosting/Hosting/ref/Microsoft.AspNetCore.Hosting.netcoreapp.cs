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
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseStaticWebAssets(this Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) { throw null; }
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
namespace Microsoft.AspNetCore.Hosting.Server.Features
{
    public partial class ServerAddressesFeature : Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature
    {
        public ServerAddressesFeature() { }
        public System.Collections.Generic.ICollection<string> Addresses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool PreferHostingUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    public partial class StaticWebAssetsLoader
    {
        public StaticWebAssetsLoader() { }
        public static void UseStaticWebAssets(Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment, Microsoft.Extensions.Configuration.IConfiguration configuration) { }
    }
}
namespace Microsoft.AspNetCore.Http
{
    public partial class DefaultHttpContextFactory : Microsoft.AspNetCore.Http.IHttpContextFactory
    {
        public DefaultHttpContextFactory(System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Http.HttpContext Create(Microsoft.AspNetCore.Http.Features.IFeatureCollection featureCollection) { throw null; }
        public void Dispose(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
    }
}
namespace Microsoft.Extensions.Hosting
{
    public static partial class GenericHostWebHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureWebHost(this Microsoft.Extensions.Hosting.IHostBuilder builder, System.Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> configure) { throw null; }
    }
}

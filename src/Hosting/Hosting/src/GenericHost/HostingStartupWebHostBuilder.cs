// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

// We use this type to capture calls to the IWebHostBuilder so the we can properly order calls to
// to GenericHostWebHostBuilder.
internal sealed class HostingStartupWebHostBuilder : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
{
    private readonly GenericWebHostBuilder _builder;
    private Action<WebHostBuilderContext, IConfigurationBuilder>? _configureConfiguration;
    private Action<WebHostBuilderContext, IServiceCollection>? _configureServices;

    public HostingStartupWebHostBuilder(GenericWebHostBuilder builder)
    {
        _builder = builder;
    }

    public IWebHost Build()
    {
        throw new NotSupportedException($"Building this implementation of {nameof(IWebHostBuilder)} is not supported.");
    }

    public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _configureConfiguration += configureDelegate;
        return this;
    }

    public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        return ConfigureServices((context, services) => configureServices(services));
    }

    public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
    {
        _configureServices += configureServices;
        return this;
    }

    public string? GetSetting(string key) => _builder.GetSetting(key);

    public IWebHostBuilder UseSetting(string key, string? value)
    {
        _builder.UseSetting(key, value);
        return this;
    }

    public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        _configureServices?.Invoke(context, services);
    }

    public void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
    {
        _configureConfiguration?.Invoke(context, builder);
    }

    public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
    {
        return _builder.UseDefaultServiceProvider(configure);
    }

    public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
    {
        return _builder.Configure(configure);
    }

    public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
    {
        return _builder.Configure(configure);
    }

    public IWebHostBuilder UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType)
    {
        return _builder.UseStartup(startupType);
    }

    // Note: This method isn't 100% compatible with trimming. It is possible for the factory to return a derived type from TStartup.
    // RequiresUnreferencedCode isn't on this method because the majority of people won't do that.
    public IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory)
    {
        return _builder.UseStartup(startupFactory);
    }
}

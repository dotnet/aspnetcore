// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes;

public class GenericWebHostBuilderWrapper : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
{
    private readonly GenericWebHostBuilder _builder;
    private readonly HostBuilder _hostBuilder;

    internal GenericWebHostBuilderWrapper(HostBuilder hostBuilder)
    {
        _builder = new GenericWebHostBuilder(hostBuilder, new WebHostBuilderOptions());
        _hostBuilder = hostBuilder;
    }

    // This is the only one that doesn't pass through
    public IWebHost Build()
    {
        _hostBuilder.ConfigureServices((context, services) => services.AddHostedService<GenericWebHostService>());
        return new GenericWebHost(_hostBuilder.Build());
    }

    public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
    {
        _builder.Configure(configure);
        return this;
    }

    public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
    {
        _builder.Configure(configure);
        return this;
    }

    public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _builder.ConfigureAppConfiguration(configureDelegate);
        return this;
    }

    public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        _builder.ConfigureServices(configureServices);
        return this;
    }

    public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
    {
        _builder.ConfigureServices(configureServices);
        return this;
    }

    public string GetSetting(string key)
    {
        return _builder.GetSetting(key);
    }

    public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
    {
        _builder.UseDefaultServiceProvider(configure);
        return this;
    }

    public IWebHostBuilder UseSetting(string key, string value)
    {
        _builder.UseSetting(key, value);
        return this;
    }

    public IWebHostBuilder UseStartup(Type startupType)
    {
        _builder.UseStartup(startupType);
        return this;
    }

    public IWebHostBuilder UseStartup<TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory)
    {
        _builder.UseStartup(startupFactory);
        return this;
    }
}

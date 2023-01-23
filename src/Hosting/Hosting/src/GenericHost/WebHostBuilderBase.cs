// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

internal abstract class WebHostBuilderBase : IWebHostBuilder, ISupportsUseDefaultServiceProvider
{
    private protected readonly IHostBuilder _builder;
    private protected readonly IConfiguration _config;

    public WebHostBuilderBase(IHostBuilder builder, WebHostBuilderOptions options)
    {
        _builder = builder;
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection();

        if (!options.SuppressEnvironmentConfiguration)
        {
            configBuilder.AddEnvironmentVariables(prefix: "ASPNETCORE_");
        }

        _config = configBuilder.Build();
    }

    public IWebHost Build()
    {
        throw new NotSupportedException($"Building this implementation of {nameof(IWebHostBuilder)} is not supported.");
    }

    public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _builder.ConfigureAppConfiguration((context, builder) =>
        {
            var webhostBuilderContext = GetWebHostBuilderContext(context);
            configureDelegate(webhostBuilderContext, builder);
        });

        return this;
    }

    public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        return ConfigureServices((context, services) => configureServices(services));
    }

    public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
    {
        _builder.ConfigureServices((context, builder) =>
        {
            var webhostBuilderContext = GetWebHostBuilderContext(context);
            configureServices(webhostBuilderContext, builder);
        });

        return this;
    }

    public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
    {
        _builder.UseServiceProviderFactory(context =>
        {
            var webHostBuilderContext = GetWebHostBuilderContext(context);
            var options = new ServiceProviderOptions();
            configure(webHostBuilderContext, options);
            return new DefaultServiceProviderFactory(options);
        });

        return this;
    }

    protected WebHostBuilderContext GetWebHostBuilderContext(HostBuilderContext context)
    {
        if (!context.Properties.TryGetValue(typeof(WebHostBuilderContext), out var contextVal))
        {
            // Use _config as a fallback for WebHostOptions in case the chained source was removed from the hosting IConfigurationBuilder.
            var options = new WebHostOptions(context.Configuration, fallbackConfiguration: _config, environment: context.HostingEnvironment);
            var webHostBuilderContext = new WebHostBuilderContext
            {
                Configuration = context.Configuration,
                HostingEnvironment = new HostingEnvironment(),
            };
            webHostBuilderContext.HostingEnvironment.Initialize(context.HostingEnvironment.ContentRootPath, options, baseEnvironment: context.HostingEnvironment);
            context.Properties[typeof(WebHostBuilderContext)] = webHostBuilderContext;
            context.Properties[typeof(WebHostOptions)] = options;
            return webHostBuilderContext;
        }

        // Refresh config, it's periodically updated/replaced
        var webHostContext = (WebHostBuilderContext)contextVal;
        webHostContext.Configuration = context.Configuration;
        return webHostContext;
    }

    public string? GetSetting(string key)
    {
        return _config[key];
    }

    public IWebHostBuilder UseSetting(string key, string? value)
    {
        _config[key] = value;
        return this;
    }
}

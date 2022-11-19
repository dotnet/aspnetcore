// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class SlimWebHostBuilder : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
{
    private readonly IHostBuilder _builder;
    private readonly IConfiguration _config;

    public SlimWebHostBuilder(IHostBuilder builder, WebHostBuilderOptions options)
    {
        _builder = builder;
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection();

        if (!options.SuppressEnvironmentConfiguration)
        {
            configBuilder.AddEnvironmentVariables(prefix: "ASPNETCORE_");
        }

        _config = configBuilder.Build();

        _builder.ConfigureHostConfiguration(config =>
        {
            config.AddConfiguration(_config);
        });

        _builder.ConfigureServices((context, services) =>
        {
            var webhostContext = GetWebHostBuilderContext(context);
            var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

            // Add the IHostingEnvironment and IApplicationLifetime from Microsoft.AspNetCore.Hosting
            services.AddSingleton(webhostContext.HostingEnvironment);
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddSingleton((AspNetCore.Hosting.IHostingEnvironment)webhostContext.HostingEnvironment);
            services.AddSingleton<IApplicationLifetime, GenericWebHostApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete

            services.Configure<GenericWebHostServiceOptions>(options =>
            {
                // Set the options
                options.WebHostOptions = webHostOptions;
            });

            // REVIEW: This is bad since we don't own this type. Anybody could add one of these and it would mess things up
            // We need to flow this differently
            services.TryAddSingleton(sp => new DiagnosticListener("Microsoft.AspNetCore"));
            services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
            services.TryAddSingleton(sp => new ActivitySource("Microsoft.AspNetCore"));
            services.TryAddSingleton(DistributedContextPropagator.Current);

            services.TryAddSingleton<IHttpContextFactory, DefaultHttpContextFactory>();
            services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
            services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();
        });
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
            // TODO: Remove when DI no longer has RequiresDynamicCodeAttribute https://github.com/dotnet/runtime/pull/79425
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            return new DefaultServiceProviderFactory(options);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        });

        return this;
    }

    public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder UseStartup([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type startupType)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory)
    {
        throw new NotSupportedException();
    }

    public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
    {
        var startupAssemblyName = configure.GetMethodInfo().DeclaringType!.Assembly.GetName().Name!;

        UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);

        _builder.ConfigureServices((context, services) =>
        {
            services.Configure<GenericWebHostServiceOptions>(options =>
            {
                var webhostBuilderContext = GetWebHostBuilderContext(context);
                options.ConfigureApplication = app => configure(webhostBuilderContext, app);
            });
        });

        return this;
    }

    private WebHostBuilderContext GetWebHostBuilderContext(HostBuilderContext context)
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

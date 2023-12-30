// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// A builder for <see cref="IWebHost"/>
/// </summary>
public class WebHostBuilder : IWebHostBuilder
{
    private readonly HostingEnvironment _hostingEnvironment;
    private readonly IConfiguration _config;
    private readonly WebHostBuilderContext _context;

    private WebHostOptions? _options;
    private bool _webHostBuilt;
    private Action<WebHostBuilderContext, IServiceCollection>? _configureServices;
    private Action<WebHostBuilderContext, IConfigurationBuilder>? _configureAppConfigurationBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebHostBuilder"/> class.
    /// </summary>
    public WebHostBuilder()
    {
        _hostingEnvironment = new HostingEnvironment();

        _config = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "ASPNETCORE_")
            .Build();

        if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.EnvironmentKey)))
        {
            // Try adding legacy environment keys, never remove these.
            UseSetting(WebHostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("Hosting:Environment")
                ?? Environment.GetEnvironmentVariable("ASPNET_ENV"));
        }

        if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.ServerUrlsKey)))
        {
            // Try adding legacy url key, never remove this.
            UseSetting(WebHostDefaults.ServerUrlsKey, Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS"));
        }

        _context = new WebHostBuilderContext
        {
            Configuration = _config
        };
    }

    /// <summary>
    /// Get the setting value from the configuration.
    /// </summary>
    /// <param name="key">The key of the setting to look up.</param>
    /// <returns>The value the setting currently contains.</returns>
    public string? GetSetting(string key)
    {
        return _config[key];
    }

    /// <summary>
    /// Add or replace a setting in the configuration.
    /// </summary>
    /// <param name="key">The key of the setting to add or replace.</param>
    /// <param name="value">The value of the setting to add or replace.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public IWebHostBuilder UseSetting(string key, string? value)
    {
        _config[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a delegate for configuring additional services for the host or web application. This may be called
    /// multiple times.
    /// </summary>
    /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        return ConfigureServices((_, services) => configureServices(services));
    }

    /// <summary>
    /// Adds a delegate for configuring additional services for the host or web application. This may be called
    /// multiple times.
    /// </summary>
    /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
    {
        _configureServices += configureServices;
        return this;
    }

    /// <summary>
    /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    /// <remarks>
    /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
    /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IWebHostBuilder"/>.
    /// </remarks>
    public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _configureAppConfigurationBuilder += configureDelegate;
        return this;
    }

    /// <summary>
    /// Builds the required services and an <see cref="IWebHost"/> which hosts a web application.
    /// </summary>
    public IWebHost Build()
    {
        if (_webHostBuilt)
        {
            throw new InvalidOperationException(Resources.WebHostBuilder_SingleInstance);
        }
        _webHostBuilt = true;

        var hostingServices = BuildCommonServices(out var hostingStartupErrors);
        var applicationServices = hostingServices.Clone();
        var hostingServiceProvider = GetProviderFromFactory(hostingServices);

        if (!_options.SuppressStatusMessages)
        {
            // Warn about deprecated environment variables
            if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
            {
                Console.WriteLine("The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
            {
                Console.WriteLine("The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS") != null)
            {
                Console.WriteLine("The environment variable 'ASPNETCORE_SERVER.URLS' is obsolete and has been replaced with 'ASPNETCORE_URLS'");
            }
        }

        AddApplicationServices(applicationServices, hostingServiceProvider);

        var host = new WebHost(
            applicationServices,
            hostingServiceProvider,
            _options,
            _config,
            hostingStartupErrors);
        try
        {
            host.Initialize();

            // resolve configuration explicitly once to mark it as resolved within the
            // service provider, ensuring it will be properly disposed with the provider
            _ = host.Services.GetService<IConfiguration>();

            var logger = host.Services.GetRequiredService<ILogger<WebHost>>();

            // Warn about duplicate HostingStartupAssemblies
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies())
            {
                if (!assemblyNames.Add(assemblyName) && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning($"The assembly {assemblyName} was specified multiple times. Hosting startup assemblies should only be specified once.");
                }
            }

            return host;
        }
        catch
        {
            // Dispose the host if there's a failure to initialize, this should dispose
            // services that were constructed until the exception was thrown
            host.Dispose();
            throw;
        }

        static IServiceProvider GetProviderFromFactory(IServiceCollection collection)
        {
            var provider = collection.BuildServiceProvider();
            var factory = provider.GetService<IServiceProviderFactory<IServiceCollection>>();

            if (factory != null && factory is not DefaultServiceProviderFactory)
            {
                using (provider)
                {
                    return factory.CreateServiceProvider(factory.CreateBuilder(collection));
                }
            }

            return provider;
        }
    }

    [MemberNotNull(nameof(_options))]
    private IServiceCollection BuildCommonServices(out AggregateException? hostingStartupErrors)
    {
        hostingStartupErrors = null;

        _options = new WebHostOptions(_config);

        if (!_options.PreventHostingStartup)
        {
            var exceptions = new List<Exception>();
            var processed = new HashSet<Assembly>();

            // Execute the hosting startup assemblies
            foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies())
            {
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(assemblyName));

                    if (!processed.Add(assembly))
                    {
                        // Already processed, skip it
                        continue;
                    }

                    foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                    {
                        var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType)!;
                        hostingStartup.Configure(this);
                    }
                }
                catch (Exception ex)
                {
                    // Capture any errors that happen during startup
                    exceptions.Add(new InvalidOperationException($"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.", ex));
                }
            }

            if (exceptions.Count > 0)
            {
                hostingStartupErrors = new AggregateException(exceptions);
            }
        }

        var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, AppContext.BaseDirectory);

        // Initialize the hosting environment
        ((IWebHostEnvironment)_hostingEnvironment).Initialize(contentRootPath, _options);
        _context.HostingEnvironment = _hostingEnvironment;

        var services = new ServiceCollection();
        services.AddSingleton(_options);
        services.AddSingleton<IWebHostEnvironment>(_hostingEnvironment);
        services.AddSingleton<IHostEnvironment>(_hostingEnvironment);
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddSingleton<AspNetCore.Hosting.IHostingEnvironment>(_hostingEnvironment);
        services.AddSingleton<Extensions.Hosting.IHostingEnvironment>(_hostingEnvironment);
#pragma warning restore CS0618 // Type or member is obsolete
        services.AddSingleton(_context);

        var builder = new ConfigurationBuilder()
            .SetBasePath(_hostingEnvironment.ContentRootPath)
            .AddConfiguration(_config, shouldDisposeConfiguration: true);

        _configureAppConfigurationBuilder?.Invoke(_context, builder);

        var configuration = builder.Build();
        // register configuration as factory to make it dispose with the service provider
        services.AddSingleton<IConfiguration>(_ => configuration);
        _context.Configuration = configuration;

        services.TryAddSingleton(sp => new DiagnosticListener("Microsoft.AspNetCore"));
        services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
        services.TryAddSingleton(sp => new ActivitySource("Microsoft.AspNetCore"));
        services.TryAddSingleton(DistributedContextPropagator.Current);

        services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
        services.AddTransient<IHttpContextFactory, DefaultHttpContextFactory>();
        services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
        services.AddOptions();
        services.AddLogging();

        services.AddMetrics();
        services.TryAddSingleton<HostingMetrics>();

        services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

        if (!string.IsNullOrEmpty(_options.StartupAssembly))
        {
            ScanAssemblyAndRegisterStartup(services, _options.StartupAssembly);
        }

        _configureServices?.Invoke(_context, services);

        return services;
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2077", Justification = "Finding startup type in assembly requires unreferenced code. Surfaced to user in UseStartup(startupAssemblyName).")]
    [UnconditionalSuppressMessage("Trimmer", "IL2072", Justification = "Finding startup type in assembly requires unreferenced code. Surfaced to user in UseStartup(startupAssemblyName).")]
    private void ScanAssemblyAndRegisterStartup(ServiceCollection services, string startupAssemblyName)
    {
        try
        {
            var startupType = StartupLoader.FindStartupType(startupAssemblyName, _hostingEnvironment.EnvironmentName);

            if (typeof(IStartup).IsAssignableFrom(startupType))
            {
                services.AddSingleton(typeof(IStartup), startupType);
            }
            else
            {
                services.AddSingleton(typeof(IStartup), RegisterStartup);

                [UnconditionalSuppressMessage("Trimmer", "IL2077", Justification = "Finding startup type in assembly requires unreferenced code. Surfaced to user in UseStartup(startupAssemblyName).")]
                object RegisterStartup(IServiceProvider serviceProvider)
                {
                    var hostingEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                    var methods = StartupLoader.LoadMethods(serviceProvider, startupType, hostingEnvironment.EnvironmentName);
                    return new ConventionBasedStartup(methods);
                }
            }
        }
        catch (Exception ex)
        {
            var capture = ExceptionDispatchInfo.Capture(ex);
            services.AddSingleton<IStartup>(_ =>
            {
                capture.Throw();
                return null;
            });
        }
    }

    private static void AddApplicationServices(IServiceCollection services, IServiceProvider hostingServiceProvider)
    {
        // We are forwarding services from hosting container so hosting container
        // can still manage their lifetime (disposal) shared instances with application services.
        // NOTE: This code overrides original services lifetime. Instances would always be singleton in
        // application container.
        var listener = hostingServiceProvider.GetService<DiagnosticListener>();
        services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticListener), listener!));
        services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticSource), listener!));

        var activitySource = hostingServiceProvider.GetService<ActivitySource>();
        services.Replace(ServiceDescriptor.Singleton(typeof(ActivitySource), activitySource!));
    }

    private static string ResolveContentRootPath(string? contentRootPath, string basePath)
    {
        if (string.IsNullOrEmpty(contentRootPath))
        {
            return basePath;
        }
        if (Path.IsPathRooted(contentRootPath))
        {
            return contentRootPath;
        }
        return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
    }
}

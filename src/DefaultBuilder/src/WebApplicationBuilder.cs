// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A builder for web applications and services.
/// </summary>
public sealed class WebApplicationBuilder : IHostApplicationBuilder
{
    private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";
    private const string AuthenticationMiddlewareSetKey = "__AuthenticationMiddlewareSet";
    private const string AuthorizationMiddlewareSetKey = "__AuthorizationMiddlewareSet";
    private const string UseRoutingKey = "__UseRouting";

    private readonly HostApplicationBuilder _hostApplicationBuilder;
    private readonly ServiceDescriptor _genericWebHostServiceDescriptor;

    private WebApplication? _builtApplication;

    internal WebApplicationBuilder(WebApplicationOptions options, Action<IHostBuilder>? configureDefaults = null)
    {
        var configuration = new ConfigurationManager();

        configuration.AddEnvironmentVariables(prefix: "ASPNETCORE_");

        _hostApplicationBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args,
            ApplicationName = options.ApplicationName,
            EnvironmentName = options.EnvironmentName,
            ContentRootPath = options.ContentRootPath,
            Configuration = configuration,
        });

        // Set WebRootPath if necessary
        if (options.WebRootPath is not null)
        {
            Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(WebHostDefaults.WebRootKey, options.WebRootPath),
            });
        }

        // Run methods to configure web host defaults early to populate services
        var bootstrapHostBuilder = new BootstrapHostBuilder(_hostApplicationBuilder);

        // This is for testing purposes
        configureDefaults?.Invoke(bootstrapHostBuilder);

        bootstrapHostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
        {
            // Runs inline.
            webHostBuilder.Configure(ConfigureApplication);

            InitializeWebHostSettings(webHostBuilder);
        },
        options =>
        {
            // We've already applied "ASPNETCORE_" environment variables to hosting config
            options.SuppressEnvironmentConfiguration = true;
        });

        _genericWebHostServiceDescriptor = InitializeHosting(bootstrapHostBuilder);
    }

    internal WebApplicationBuilder(WebApplicationOptions options, bool slim, Action<IHostBuilder>? configureDefaults = null)
    {
        Debug.Assert(slim, "should only be called with slim: true");

        var configuration = new ConfigurationManager();

        configuration.AddEnvironmentVariables(prefix: "ASPNETCORE_");

        // SetDefaultContentRoot needs to be added between 'ASPNETCORE_' and 'DOTNET_' in order to match behavior of the non-slim WebApplicationBuilder.
        SetDefaultContentRoot(options, configuration);

        // Add the default host environment variable configuration source.
        // This won't be added by CreateEmptyApplicationBuilder.
        configuration.AddEnvironmentVariables(prefix: "DOTNET_");

        _hostApplicationBuilder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args,
            ApplicationName = options.ApplicationName,
            EnvironmentName = options.EnvironmentName,
            ContentRootPath = options.ContentRootPath,
            Configuration = configuration,
        });

        // Ensure the same behavior of the non-slim WebApplicationBuilder by adding the default "app" Configuration sources
        ApplyDefaultAppConfigurationSlim(_hostApplicationBuilder.Environment, configuration, options.Args);
        AddDefaultServicesSlim(configuration, _hostApplicationBuilder.Services);

        // configure the ServiceProviderOptions here since CreateEmptyApplicationBuilder won't.
        var serviceProviderFactory = GetServiceProviderFactory(_hostApplicationBuilder);
        _hostApplicationBuilder.ConfigureContainer(serviceProviderFactory);

        // Set WebRootPath if necessary
        if (options.WebRootPath is not null)
        {
            Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(WebHostDefaults.WebRootKey, options.WebRootPath),
            });
        }

        // Run methods to configure web host defaults early to populate services
        var bootstrapHostBuilder = new BootstrapHostBuilder(_hostApplicationBuilder);

        // This is for testing purposes
        configureDefaults?.Invoke(bootstrapHostBuilder);

        bootstrapHostBuilder.ConfigureSlimWebHost(
            webHostBuilder =>
            {
                AspNetCore.WebHost.ConfigureWebDefaultsSlim(webHostBuilder);

                // Runs inline.
                webHostBuilder.Configure(ConfigureApplication);

                InitializeWebHostSettings(webHostBuilder);
            },
            options =>
            {
                // We've already applied "ASPNETCORE_" environment variables to hosting config
                options.SuppressEnvironmentConfiguration = true;
            });

        _genericWebHostServiceDescriptor = InitializeHosting(bootstrapHostBuilder);
    }

    internal WebApplicationBuilder(WebApplicationOptions options, bool slim, bool empty, Action<IHostBuilder>? configureDefaults = null)
    {
        Debug.Assert(!slim, "should only be called with slim: false");
        Debug.Assert(empty, "should only be called with empty: true");

        var configuration = new ConfigurationManager();

        // empty builder should still default the ContentRoot as usual. This is the expected behavior for all WebApplicationBuilders.
        SetDefaultContentRoot(options, configuration);

        _hostApplicationBuilder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args,
            ApplicationName = options.ApplicationName,
            EnvironmentName = options.EnvironmentName,
            ContentRootPath = options.ContentRootPath,
            Configuration = configuration,
        });

        // Set WebRootPath if necessary
        if (options.WebRootPath is not null)
        {
            Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(WebHostDefaults.WebRootKey, options.WebRootPath),
            });
        }

        // Run methods to configure web host defaults early to populate services
        var bootstrapHostBuilder = new BootstrapHostBuilder(_hostApplicationBuilder);

        // This is for testing purposes
        configureDefaults?.Invoke(bootstrapHostBuilder);

        bootstrapHostBuilder.ConfigureSlimWebHost(
            webHostBuilder =>
            {
                // Note this doesn't configure any WebHost server - Kestrel or otherwise.
                // It also doesn't register Routing, HostFiltering, or ForwardedHeaders.
                // It is "empty" and up to the caller to configure these services.

                // Runs inline.
                webHostBuilder.Configure((context, app) => ConfigureApplication(context, app, allowDeveloperExceptionPage: false));

                InitializeWebHostSettings(webHostBuilder);
            },
            options =>
            {
                // This is an "empty" builder, so don't add the "ASPNETCORE_" environment variables
                options.SuppressEnvironmentConfiguration = true;
            });

        _genericWebHostServiceDescriptor = InitializeHosting(bootstrapHostBuilder);
    }

    [MemberNotNull(nameof(Environment), nameof(Host), nameof(WebHost))]
    private ServiceDescriptor InitializeHosting(BootstrapHostBuilder bootstrapHostBuilder)
    {
        // This applies the config from ConfigureWebHostDefaults
        // Grab the GenericWebHostService ServiceDescriptor so we can append it after any user-added IHostedServices during Build();
        var genericWebHostServiceDescriptor = bootstrapHostBuilder.RunDefaultCallbacks();

        // Grab the WebHostBuilderContext from the property bag to use in the ConfigureWebHostBuilder. Then
        // grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
        var webHostContext = (WebHostBuilderContext)bootstrapHostBuilder.Properties[typeof(WebHostBuilderContext)];
        Environment = webHostContext.HostingEnvironment;

        Host = new ConfigureHostBuilder(bootstrapHostBuilder.Context, Configuration, Services);
        WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);

        return genericWebHostServiceDescriptor;
    }

    private void InitializeWebHostSettings(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, _hostApplicationBuilder.Environment.ApplicationName ?? "");
        webHostBuilder.UseSetting(WebHostDefaults.PreventHostingStartupKey, Configuration[WebHostDefaults.PreventHostingStartupKey]);
        webHostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, Configuration[WebHostDefaults.HostingStartupAssembliesKey]);
        webHostBuilder.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, Configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);
    }

    private static DefaultServiceProviderFactory GetServiceProviderFactory(HostApplicationBuilder hostApplicationBuilder)
    {
        if (hostApplicationBuilder.Environment.IsDevelopment())
        {
            return new DefaultServiceProviderFactory(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true,
                });
        }

        return new DefaultServiceProviderFactory();
    }

    private static void SetDefaultContentRoot(WebApplicationOptions options, ConfigurationManager configuration)
    {
        if (options.ContentRootPath is null && configuration[HostDefaults.ContentRootKey] is null)
        {
            // Logic taken from https://github.com/dotnet/runtime/blob/dc5a6c8be1644915c14c4a464447b0d54e223a46/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L209-L227

            // If we're running anywhere other than C:\Windows\system32, we default to using the CWD for the ContentRoot.
            // However, since many things like Windows services and MSIX installers have C:\Windows\system32 as their CWD, which is not likely
            // to be the home for things like appsettings.json, we skip changing the ContentRoot in that case. The non-"default" initial
            // value for ContentRoot is AppContext.BaseDirectory (e.g. the executable path) which probably makes more sense than system32.

            // In my testing, both Environment.CurrentDirectory and Environment.SystemDirectory return the path without
            // any trailing directory separator characters. I'm not even sure the casing can ever be different from these APIs, but I think it makes sense to
            // ignore case for Windows path comparisons given the file system is usually (always?) going to be case insensitive for the system path.
            string cwd = System.Environment.CurrentDirectory;
            if (!OperatingSystem.IsWindows() || !string.Equals(cwd, System.Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase))
            {
                configuration.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>(HostDefaults.ContentRootKey, cwd),
                });
            }
        }
    }

    private static void ApplyDefaultAppConfigurationSlim(IHostEnvironment env, ConfigurationManager configuration, string[]? args)
    {
        // Logic taken from https://github.com/dotnet/runtime/blob/6149ca07d2202c2d0d518e10568c0d0dd3473576/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L229-L256

        var reloadOnChange = GetReloadConfigOnChangeValue(configuration);

        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: reloadOnChange)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);

        if (env.IsDevelopment() && env.ApplicationName is { Length: > 0 })
        {
            try
            {
                var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                configuration.AddUserSecrets(appAssembly, optional: true, reloadOnChange: reloadOnChange);
            }
            catch (FileNotFoundException)
            {
                // The assembly cannot be found, so just skip it.
            }
        }

        configuration.AddEnvironmentVariables();

        if (args is { Length: > 0 })
        {
            configuration.AddCommandLine(args);
        }

        static bool GetReloadConfigOnChangeValue(ConfigurationManager configuration)
        {
            const string reloadConfigOnChangeKey = "hostBuilder:reloadConfigOnChange";
            var result = true;
            if (configuration[reloadConfigOnChangeKey] is string reloadConfigOnChange)
            {
                if (!bool.TryParse(reloadConfigOnChange, out result))
                {
                    throw new InvalidOperationException($"Failed to convert configuration value at '{configuration.GetSection(reloadConfigOnChangeKey).Path}' to type '{typeof(bool)}'.");
                }
            }
            return result;
        }
    }

    private static void AddDefaultServicesSlim(ConfigurationManager configuration, IServiceCollection services)
    {
        // Add the necessary services for the slim WebApplicationBuilder, taken from https://github.com/dotnet/runtime/blob/6149ca07d2202c2d0d518e10568c0d0dd3473576/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L266
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.AddSimpleConsole();

            logging.Configure(options =>
            {
                options.ActivityTrackingOptions =
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.ParentId;
            });
        });
    }

    /// <summary>
    /// Provides information about the web hosting environment an application is running.
    /// </summary>
    public IWebHostEnvironment Environment { get; private set; }

    /// <summary>
    /// A collection of services for the application to compose. This is useful for adding user provided or framework provided services.
    /// </summary>
    public IServiceCollection Services => _hostApplicationBuilder.Services;

    /// <summary>
    /// A collection of configuration providers for the application to compose. This is useful for adding new configuration sources and providers.
    /// </summary>
    public ConfigurationManager Configuration => _hostApplicationBuilder.Configuration;

    /// <summary>
    /// A collection of logging providers for the application to compose. This is useful for adding new logging providers.
    /// </summary>
    public ILoggingBuilder Logging => _hostApplicationBuilder.Logging;

    /// <summary>
    /// Allows enabling metrics and directing their output.
    /// </summary>
    public IMetricsBuilder Metrics => _hostApplicationBuilder.Metrics;

    /// <summary>
    /// An <see cref="IWebHostBuilder"/> for configuring server specific properties, but not building.
    /// To build after configuration, call <see cref="Build"/>.
    /// </summary>
    public ConfigureWebHostBuilder WebHost { get; private set; }

    /// <summary>
    /// An <see cref="IHostBuilder"/> for configuring host specific properties, but not building.
    /// To build after configuration, call <see cref="Build"/>.
    /// </summary>
    public ConfigureHostBuilder Host { get; private set; }

    IDictionary<object, object> IHostApplicationBuilder.Properties => ((IHostApplicationBuilder)_hostApplicationBuilder).Properties;

    IConfigurationManager IHostApplicationBuilder.Configuration => Configuration;

    IHostEnvironment IHostApplicationBuilder.Environment => Environment;

    /// <summary>
    /// Builds the <see cref="WebApplication"/>.
    /// </summary>
    /// <returns>A configured <see cref="WebApplication"/>.</returns>
    public WebApplication Build()
    {
        // ConfigureContainer callbacks run after ConfigureServices callbacks including the one that adds GenericWebHostService by default.
        // One nice side effect is this gives a way to configure an IHostedService that starts after the server and stops beforehand.
        _hostApplicationBuilder.Services.Add(_genericWebHostServiceDescriptor);
        Host.ApplyServiceProviderFactory(_hostApplicationBuilder);
        _builtApplication = new WebApplication(_hostApplicationBuilder.Build());
        return _builtApplication;
    }

    private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app) =>
        ConfigureApplication(context, app, allowDeveloperExceptionPage: true);

    private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app, bool allowDeveloperExceptionPage)
    {
        Debug.Assert(_builtApplication is not null);

        // UseRouting called before WebApplication such as in a StartupFilter
        // lets remove the property and reset it at the end so we don't mess with the routes in the filter
        if (app.Properties.TryGetValue(EndpointRouteBuilderKey, out var priorRouteBuilder))
        {
            app.Properties.Remove(EndpointRouteBuilderKey);
        }

        if (allowDeveloperExceptionPage && context.HostingEnvironment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Wrap the entire destination pipeline in UseRouting() and UseEndpoints(), essentially:
        // destination.UseRouting()
        // destination.Run(source)
        // destination.UseEndpoints()

        // Set the route builder so that UseRouting will use the WebApplication as the IEndpointRouteBuilder for route matching
        app.Properties.Add(WebApplication.GlobalEndpointRouteBuilderKey, _builtApplication);

        // Only call UseRouting() if there are endpoints configured and UseRouting() wasn't called on the global route builder already
        if (_builtApplication.DataSources.Count > 0)
        {
            // If this is set, someone called UseRouting() when a global route builder was already set
            if (!_builtApplication.Properties.TryGetValue(EndpointRouteBuilderKey, out var localRouteBuilder))
            {
                app.UseRouting();
                // Middleware the needs to re-route will use this property to call UseRouting()
                _builtApplication.Properties[UseRoutingKey] = app.Properties[UseRoutingKey];
            }
            else
            {
                // UseEndpoints will be looking for the RouteBuilder so make sure it's set
                app.Properties[EndpointRouteBuilderKey] = localRouteBuilder;
            }
        }

        // Process authorization and authentication middlewares independently to avoid
        // registering middlewares for services that do not exist
        var serviceProviderIsService = _builtApplication.Services.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService?.IsService(typeof(IAuthenticationSchemeProvider)) is true)
        {
            // Don't add more than one instance of the middleware
            if (!_builtApplication.Properties.ContainsKey(AuthenticationMiddlewareSetKey))
            {
                // The Use invocations will set the property on the outer pipeline,
                // but we want to set it on the inner pipeline as well.
                _builtApplication.Properties[AuthenticationMiddlewareSetKey] = true;
                app.UseAuthentication();
            }
        }

        if (serviceProviderIsService?.IsService(typeof(IAuthorizationHandlerProvider)) is true)
        {
            if (!_builtApplication.Properties.ContainsKey(AuthorizationMiddlewareSetKey))
            {
                _builtApplication.Properties[AuthorizationMiddlewareSetKey] = true;
                app.UseAuthorization();
            }
        }

        // Wire the source pipeline to run in the destination pipeline
        var wireSourcePipeline = new WireSourcePipeline(_builtApplication);
        app.Use(wireSourcePipeline.CreateMiddleware);

        if (_builtApplication.DataSources.Count > 0)
        {
            // We don't know if user code called UseEndpoints(), so we will call it just in case, UseEndpoints() will ignore duplicate DataSources
            app.UseEndpoints(_ => { });
        }

        MergeMiddlewareDescriptions(app);

        // Copy the properties to the destination app builder
        foreach (var item in _builtApplication.Properties)
        {
            app.Properties[item.Key] = item.Value;
        }

        // Remove the route builder to clean up the properties, we're done adding routes to the pipeline
        app.Properties.Remove(WebApplication.GlobalEndpointRouteBuilderKey);

        // Reset route builder if it existed, this is needed for StartupFilters
        if (priorRouteBuilder is not null)
        {
            app.Properties[EndpointRouteBuilderKey] = priorRouteBuilder;
        }
    }

    void IHostApplicationBuilder.ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure) =>
        _hostApplicationBuilder.ConfigureContainer(factory, configure);

    private void MergeMiddlewareDescriptions(IApplicationBuilder app)
    {
        // A user's app builds up a list of middleware. Then when the WebApplication is started, middleware is automatically added
        // if it is required. For example, the app has mapped endpoints but hasn't configured UseRouting/UseEndpoints.
        //
        // This method updates the middleware descriptions to include automatically added middleware.
        // The app's middleware list is inserted into the new pipeline to create the best representation possible of the middleware pipeline.
        //
        // If the debugger isn't attached then there won't be middleware description collections in the properties and this does nothing.

        Debug.Assert(_builtApplication is not null);

        const string MiddlewareDescriptionsKey = "__MiddlewareDescriptions";
        if (_builtApplication.Properties.TryGetValue(MiddlewareDescriptionsKey, out var sourceValue) &&
            app.Properties.TryGetValue(MiddlewareDescriptionsKey, out var destinationValue) &&
            sourceValue is List<string> sourceDescriptions &&
            destinationValue is List<string> destinationDescriptions)
        {
            var wireUpIndex = destinationDescriptions.IndexOf(typeof(WireSourcePipeline).FullName!);
            if (wireUpIndex != -1)
            {
                destinationDescriptions.RemoveAt(wireUpIndex);
                destinationDescriptions.InsertRange(wireUpIndex, sourceDescriptions);

                _builtApplication.Properties[MiddlewareDescriptionsKey] = destinationDescriptions;
            }
        }
    }

    // This type exists so the place where the source pipeline is wired into the destination pipeline can be identified.
    private sealed class WireSourcePipeline(IApplicationBuilder builtApplication)
    {
        private readonly IApplicationBuilder _builtApplication = builtApplication;

        public RequestDelegate CreateMiddleware(RequestDelegate next)
        {
            _builtApplication.Run(next);
            return _builtApplication.Build();
        }
    }
}

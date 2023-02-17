// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A builder for web applications and services.
/// </summary>
public sealed class WebApplicationBuilder
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

            webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, _hostApplicationBuilder.Environment.ApplicationName ?? "");
            webHostBuilder.UseSetting(WebHostDefaults.PreventHostingStartupKey, Configuration[WebHostDefaults.PreventHostingStartupKey]);
            webHostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, Configuration[WebHostDefaults.HostingStartupAssembliesKey]);
            webHostBuilder.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, Configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);
        },
        options =>
        {
            // We've already applied "ASPNETCORE_" environment variables to hosting config
            options.SuppressEnvironmentConfiguration = true;
        });

        // This applies the config from ConfigureWebHostDefaults
        // Grab the GenericWebHostService ServiceDescriptor so we can append it after any user-added IHostedServices during Build();
        _genericWebHostServiceDescriptor = bootstrapHostBuilder.RunDefaultCallbacks();

        // Grab the WebHostBuilderContext from the property bag to use in the ConfigureWebHostBuilder. Then
        // grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
        var webHostContext = (WebHostBuilderContext)bootstrapHostBuilder.Properties[typeof(WebHostBuilderContext)];
        Environment = webHostContext.HostingEnvironment;

        Host = new ConfigureHostBuilder(bootstrapHostBuilder.Context, Configuration, Services);
        WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);
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
        ApplyDefaultAppConfiguration(options, configuration);

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
                AspNetCore.WebHost.ConfigureWebDefaultsCore(webHostBuilder);

                webHostBuilder.Configure(ConfigureEmptyApplication);

                webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, _hostApplicationBuilder.Environment.ApplicationName ?? "");
                webHostBuilder.UseSetting(WebHostDefaults.PreventHostingStartupKey, Configuration[WebHostDefaults.PreventHostingStartupKey]);
                webHostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, Configuration[WebHostDefaults.HostingStartupAssembliesKey]);
                webHostBuilder.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, Configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);
            },
            options =>
            {
                // We've already applied "ASPNETCORE_" environment variables to hosting config
                options.SuppressEnvironmentConfiguration = true;
            });

        // This applies the config from ConfigureWebHostDefaults
        // Grab the GenericWebHostService ServiceDescriptor so we can append it after any user-added IHostedServices during Build();
        _genericWebHostServiceDescriptor = bootstrapHostBuilder.RunDefaultCallbacks();

        // Grab the WebHostBuilderContext from the property bag to use in the ConfigureWebHostBuilder. Then
        // grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
        var webHostContext = (WebHostBuilderContext)bootstrapHostBuilder.Properties[typeof(WebHostBuilderContext)];
        Environment = webHostContext.HostingEnvironment;

        Host = new ConfigureHostBuilder(bootstrapHostBuilder.Context, Configuration, Services);
        WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);
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
            // Logic taken from https://github.com/dotnet/runtime/blob/78ed4438a42acab80541e9bde1910abaa8841db2/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L209-L227

            // If we're running anywhere other than C:\Windows\system32, we default to using the CWD for the ContentRoot.
            // However, since many things like Windows services and MSIX installers have C:\Windows\system32 as there CWD which is not likely
            // to really be the home for things like appsettings.json, we skip changing the ContentRoot in that case. The non-"default" initial
            // value for ContentRoot is AppContext.BaseDirectory (e.g. the executable path) which probably makes more sense than the system32.

            // In my testing, both Environment.CurrentDirectory and Environment.GetFolderPath(Environment.SpecialFolder.System) return the path without
            // any trailing directory separator characters. I'm not even sure the casing can ever be different from these APIs, but I think it makes sense to
            // ignore case for Windows path comparisons given the file system is usually (always?) going to be case insensitive for the system path.
            string cwd = System.Environment.CurrentDirectory;
            if (!OperatingSystem.IsWindows() || !string.Equals(cwd, System.Environment.GetFolderPath(System.Environment.SpecialFolder.System), StringComparison.OrdinalIgnoreCase))
            {
                configuration.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>(HostDefaults.ContentRootKey, cwd),
                });
            }
        }
    }

    private static void ApplyDefaultAppConfiguration(WebApplicationOptions options, ConfigurationManager configuration)
    {
        configuration.AddEnvironmentVariables();

        if (options.Args is { Length: > 0 } args)
        {
            configuration.AddCommandLine(args);
        }
    }

    /// <summary>
    /// Provides information about the web hosting environment an application is running.
    /// </summary>
    public IWebHostEnvironment Environment { get; }

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
    /// An <see cref="IWebHostBuilder"/> for configuring server specific properties, but not building.
    /// To build after configuration, call <see cref="Build"/>.
    /// </summary>
    public ConfigureWebHostBuilder WebHost { get; }

    /// <summary>
    /// An <see cref="IHostBuilder"/> for configuring host specific properties, but not building.
    /// To build after configuration, call <see cref="Build"/>.
    /// </summary>
    public ConfigureHostBuilder Host { get; }

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

    private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app)
    {
        ConfigureApplicationCore(
            context,
            app,
            processAuthMiddlewares: () =>
            {
                Debug.Assert(_builtApplication is not null);

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
            });
    }

    private void ConfigureEmptyApplication(WebHostBuilderContext context, IApplicationBuilder app)
    {
        ConfigureApplicationCore(context, app, processAuthMiddlewares: null);
    }

    private void ConfigureApplicationCore(WebHostBuilderContext context, IApplicationBuilder app, Action? processAuthMiddlewares)
    {
        Debug.Assert(_builtApplication is not null);

        // UseRouting called before WebApplication such as in a StartupFilter
        // lets remove the property and reset it at the end so we don't mess with the routes in the filter
        if (app.Properties.TryGetValue(EndpointRouteBuilderKey, out var priorRouteBuilder))
        {
            app.Properties.Remove(EndpointRouteBuilderKey);
        }

        if (context.HostingEnvironment.IsDevelopment())
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

        processAuthMiddlewares?.Invoke();

        // Wire the source pipeline to run in the destination pipeline
        app.Use(next =>
        {
            _builtApplication.Run(next);
            return _builtApplication.BuildRequestDelegate();
        });

        if (_builtApplication.DataSources.Count > 0)
        {
            // We don't know if user code called UseEndpoints(), so we will call it just in case, UseEndpoints() will ignore duplicate DataSources
            app.UseEndpoints(_ => { });
        }

        // Copy the properties to the destination app builder
        foreach (var item in _builtApplication.Properties)
        {
            app.Properties[item.Key] = item.Value;
        }

        // Remove the route builder to clean up the properties, we're done adding routes to the pipeline
        app.Properties.Remove(WebApplication.GlobalEndpointRouteBuilderKey);

        // reset route builder if it existed, this is needed for StartupFilters
        if (priorRouteBuilder is not null)
        {
            app.Properties[EndpointRouteBuilderKey] = priorRouteBuilder;
        }
    }
}

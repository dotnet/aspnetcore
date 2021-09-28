// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// A builder for web applications and services.
    /// </summary>
    public sealed class WebApplicationBuilder
    {
        private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";

        private readonly HostBuilder _hostBuilder = new();
        private readonly BootstrapHostBuilder _bootstrapHostBuilder;
        private readonly WebApplicationServiceCollection _services = new();
        private readonly List<KeyValuePair<string, string>> _hostConfigurationValues;

        private WebApplication? _builtApplication;

        internal WebApplicationBuilder(WebApplicationOptions options, Action<IHostBuilder>? configureDefaults = null)
        {
            Services = _services;

            var args = options.Args;

            // Run methods to configure both generic and web host defaults early to populate config from appsettings.json
            // environment variables (both DOTNET_ and ASPNETCORE_ prefixed) and other possible default sources to prepopulate
            // the correct defaults.
            _bootstrapHostBuilder = new BootstrapHostBuilder(Services, _hostBuilder.Properties);

            // Don't specify the args here since we want to apply them later so that args
            // can override the defaults specified by ConfigureWebHostDefaults
            _bootstrapHostBuilder.ConfigureDefaults(args: null);

            // This is for testing purposes
            configureDefaults?.Invoke(_bootstrapHostBuilder);

            // We specify the command line here last since we skipped the one in the call to ConfigureDefaults.
            // The args can contain both host and application settings so we want to make sure
            // we order those configuration providers appropriately without duplicating them
            if (args is { Length: > 0 })
            {
                _bootstrapHostBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddCommandLine(args);
                });
            }

            _bootstrapHostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
            {
                // Runs inline.
                webHostBuilder.Configure(ConfigureApplication);

                // Attempt to set the application name from options
                options.ApplyApplicationName(webHostBuilder);
            });

            // Apply the args to host configuration last since ConfigureWebHostDefaults overrides a host specific setting (the application name).
            _bootstrapHostBuilder.ConfigureHostConfiguration(config =>
            {
                if (args is { Length: > 0 })
                {
                    config.AddCommandLine(args);
                }

                // Apply the options after the args
                options.ApplyHostConfiguration(config);
            });

            Configuration = new();

            // Collect the hosted services separately since we want those to run after the user's hosted services
            _services.TrackHostedServices = true;

            // This is the application configuration
            var (hostContext, hostConfiguration) = _bootstrapHostBuilder.RunDefaultCallbacks(Configuration, _hostBuilder);

            // Stop tracking here
            _services.TrackHostedServices = false;

            // Capture the host configuration values here. We capture the values so that
            // changes to the host configuration have no effect on the final application. The
            // host configuration is immutable at this point.
            _hostConfigurationValues = new(hostConfiguration.AsEnumerable());

            // Grab the WebHostBuilderContext from the property bag to use in the ConfigureWebHostBuilder
            var webHostContext = (WebHostBuilderContext)hostContext.Properties[typeof(WebHostBuilderContext)];

            // Grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
            Environment = webHostContext.HostingEnvironment;
            Logging = new LoggingBuilder(Services);
            Host = new ConfigureHostBuilder(hostContext, Configuration, Services);
            WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);

            Services.AddSingleton<IConfiguration>(_ => Configuration);
        }

        /// <summary>
        /// Provides information about the web hosting environment an application is running.
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// A collection of services for the application to compose. This is useful for adding user provided or framework provided services.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// A collection of configuration providers for the application to compose. This is useful for adding new configuration sources and providers.
        /// </summary>
        public ConfigurationManager Configuration { get; }

        /// <summary>
        /// A collection of logging providers for the application to compose. This is useful for adding new logging providers.
        /// </summary>
        public ILoggingBuilder Logging { get; }

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
            // Wire up the host configuration here. We don't try to preserve the configuration
            // source itself here since we don't support mutating the host values after creating the builder.
            _hostBuilder.ConfigureHostConfiguration(builder =>
            {
                builder.AddInMemoryCollection(_hostConfigurationValues);
            });

            var chainedConfigSource = new TrackingChainedConfigurationSource(Configuration);

            // Wire up the application configuration by copying the already built configuration providers over to final configuration builder.
            // We wrap the existing provider in a configuration source to avoid re-bulding the already added configuration sources.
            _hostBuilder.ConfigureAppConfiguration(builder =>
            {
                builder.Add(chainedConfigSource);

                foreach (var (key, value) in ((IConfigurationBuilder)Configuration).Properties)
                {
                    builder.Properties[key] = value;
                }
            });

            // This needs to go here to avoid adding the IHostedService that boots the server twice (the GenericWebHostService).
            // Copy the services that were added via WebApplicationBuilder.Services into the final IServiceCollection
            _hostBuilder.ConfigureServices((context, services) =>
            {
                // We've only added services configured by the GenericWebHostBuilder and WebHost.ConfigureWebDefaults
                // at this point. HostBuilder news up a new ServiceCollection in HostBuilder.Build() we haven't seen
                // until now, so we cannot clear these services even though some are redundant because
                // we called ConfigureWebHostDefaults on both the _deferredHostBuilder and _hostBuilder.
                foreach (var s in _services)
                {
                    services.Add(s);
                }

                // Add the hosted services that were initially added last
                // this makes sure any hosted services that are added run after the initial set
                // of hosted services. This means hosted services run before the web host starts.
                foreach (var s in _services.HostedServices)
                {
                    services.Add(s);
                }

                // Clear the hosted services list out
                _services.HostedServices.Clear();

                // Add any services to the user visible service collection so that they are observable
                // just in case users capture the Services property. Orchard does this to get a "blueprint"
                // of the service collection

                // Drop the reference to the existing collection and set the inner collection
                // to the new one. This allows code that has references to the service collection to still function.
                _services.InnerCollection = services;

                var hostBuilderProviders = ((IConfigurationRoot)context.Configuration).Providers;

                if (!hostBuilderProviders.Contains(chainedConfigSource.BuiltProvider))
                {
                    // Something removed the _hostBuilder's TrackingChainedConfigurationSource pointing back to the ConfigurationManager.
                    // This is likely a test using WebApplicationFactory. Replicate the effect by clearing the ConfingurationManager sources.
                    ((IConfigurationBuilder)Configuration).Sources.Clear();
                }

                // Make builder.Configuration match the final configuration. To do that, we add the additional
                // providers in the inner _hostBuilders's Configuration to the ConfigurationManager.
                foreach (var provider in hostBuilderProviders)
                {
                    if (!ReferenceEquals(provider, chainedConfigSource.BuiltProvider))
                    {
                        ((IConfigurationBuilder)Configuration).Add(new ConfigurationProviderSource(provider));
                    }
                }
            });

            // Run the other callbacks on the final host builder
            Host.RunDeferredCallbacks(_hostBuilder);

            _builtApplication = new WebApplication(_hostBuilder.Build());

            // Mark the service collection as read-only to prevent future modifications
            _services.IsReadOnly = true;

            // Resolve both the _hostBuilder's Configuration and builder.Configuration to mark both as resolved within the
            // service provider ensuring both will be properly disposed with the provider.
            _ = _builtApplication.Services.GetService<IEnumerable<IConfiguration>>();

            return _builtApplication;
        }

        private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app)
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
                }
                else
                {
                    // UseEndpoints will be looking for the RouteBuilder so make sure it's set
                    app.Properties[EndpointRouteBuilderKey] = localRouteBuilder;
                }
            }

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

        private sealed class LoggingBuilder : ILoggingBuilder
        {
            public LoggingBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}

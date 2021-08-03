// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
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

        private WebApplication? _builtApplication;

        internal WebApplicationBuilder(Assembly? callingAssembly, string[]? args = null, Action<IHostBuilder>? configureDefaults = null)
        {
            Services = _services;

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

                // We need to override the application name since the call to Configure will set it to
                // be the calling assembly's name.
                webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, (callingAssembly ?? Assembly.GetEntryAssembly())?.GetName()?.Name ?? string.Empty);
            });

            // Apply the args to host configuration last since ConfigureWebHostDefaults overrides a host specific setting (the application name).
            if (args is { Length: > 0 })
            {
                _bootstrapHostBuilder.ConfigureHostConfiguration(config =>
                {
                    config.AddCommandLine(args);
                });
            }

            Configuration = new();

            // This is the application configuration
            var hostContext = _bootstrapHostBuilder.RunDefaultCallbacks(Configuration, _hostBuilder);

            // Grab the WebHostBuilderContext from the property bag to use in the ConfigureWebHostBuilder
            var webHostContext = (WebHostBuilderContext)_hostBuilder.Properties[typeof(WebHostBuilderContext)];

            // Grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
            Environment = webHostContext.HostingEnvironment;
            Logging = new LoggingBuilder(Services);
            Host = new ConfigureHostBuilder(hostContext, Configuration, Services);
            WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);
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
            // Copy the configuration sources into the final IConfigurationBuilder
            _hostBuilder.ConfigureHostConfiguration(builder =>
            {
                foreach (var source in ((IConfigurationBuilder)Configuration).Sources)
                {
                    builder.Sources.Add(source);
                }

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

                // Add any services to the user visible service collection so that they are observable
                // just in case users capture the Services property. Orchard does this to get a "blueprint"
                // of the service collection

                // Drop the reference to the existing collection and set the inner collection
                // to the new one. This allows code that has references to the service collection to still function.
                _services.InnerCollection = services;
            });

            // Run the other callbacks on the final host builder
            Host.RunDeferredCallbacks(_hostBuilder);

            _builtApplication = new WebApplication(_hostBuilder.Build());

            // Make builder.Configuration match the final configuration. To do that
            // we clear the sources and add the built configuration as a source
            ((IConfigurationBuilder)Configuration).Sources.Clear();
            Configuration.AddConfiguration(_builtApplication.Configuration);

            return _builtApplication;
        }

        private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app)
        {
            Debug.Assert(_builtApplication is not null);

            if (context.HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var implicitRouting = false;

            // The endpoints were already added on the outside
            if (_builtApplication.DataSources.Count > 0)
            {
                // The user did not register the routing middleware so wrap the entire
                // destination pipeline in UseRouting() and UseEndpoints(), essentially:
                // destination.UseRouting()
                // destination.Run(source)
                // destination.UseEndpoints()

                // Copy endpoints to the IEndpointRouteBuilder created by an explicit call to UseRouting() if possible.
                var targetRouteBuilder = GetEndpointRouteBuilder(_builtApplication);

                if (targetRouteBuilder is null)
                {
                    // The app defined endpoints without calling UseRouting() explicitly, so call UseRouting() implicitly.
                    app.UseRouting();

                    // An implicitly created IEndpointRouteBuilder was addeded to app.Properties by the UseRouting() call above.
                    targetRouteBuilder = GetEndpointRouteBuilder(app)!;
                    implicitRouting = true;
                }

                // Copy the endpoints to the explicitly or implicitly created IEndopintRouteBuilder.
                foreach (var ds in _builtApplication.DataSources)
                {
                    targetRouteBuilder.DataSources.Add(ds);
                }

                // UseEndpoints consumes the DataSources immediately to populate CompositeEndpointDataSource via RouteOptions,
                // so it must be called after we copy the endpoints.
                if (!implicitRouting)
                {
                    // UseRouting() was called explicitely, but we may still need to call UseEndpoints() implicitely at
                    // the end of the pipeline.
                    _builtApplication.UseEndpoints(_ => { });
                }
            }

            // Wire the source pipeline to run in the destination pipeline
            app.Use(next =>
            {
                _builtApplication.Run(next);
                return _builtApplication.BuildRequestDelegate();
            });

            // Implicitly call UseEndpoints() at the end of the pipeline if UseRouting() was called implicitly.
            // We could add this to the end of _buildApplication instead if UseEndpoints() was not so picky about
            // being called with the same IApplicationBluilder instance as UseRouting().
            if (implicitRouting)
            {
                app.UseEndpoints(_ => { });
            }

            // Copy the properties to the destination app builder
            foreach (var item in _builtApplication.Properties)
            {
                app.Properties[item.Key] = item.Value;
            }
        }

        private static IEndpointRouteBuilder? GetEndpointRouteBuilder(IApplicationBuilder app)
        {
            app.Properties.TryGetValue(EndpointRouteBuilderKey, out var value);
            return (IEndpointRouteBuilder?)value;
        }

        private class LoggingBuilder : ILoggingBuilder
        {
            public LoggingBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}

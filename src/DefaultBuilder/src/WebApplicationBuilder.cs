// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private readonly HostBuilder _hostBuilder = new();
        private readonly ConfigureHostBuilder _deferredHostBuilder;
        private readonly ConfigureWebHostBuilder _deferredWebHostBuilder;
        private readonly WebHostEnvironment _environment;
        private WebApplication? _builtApplication;

        internal WebApplicationBuilder(Assembly? callingAssembly, string[]? args = null)
        {
            // HACK: MVC and Identity do this horrible thing to get the hosting environment as an instance
            // from the service collection before it is built. That needs to be fixed...
            Environment = _environment = new WebHostEnvironment(callingAssembly);

            Configuration.SetBasePath(_environment.ContentRootPath);
            Services.AddSingleton(Environment);

            // Run methods to configure both generic and web host defaults early to populate config from appsettings.json
            // environment variables (both DOTNET_ and ASPNETCORE_ prefixed) and other possible default sources to prepopulate
            // the correct defaults.
            var bootstrapBuilder = new BootstrapHostBuilder(Configuration, _environment);
            bootstrapBuilder.ConfigureDefaults(args);
            bootstrapBuilder.ConfigureWebHostDefaults(configure: _ => { });
            bootstrapBuilder.RunConfigurationCallbacks();

            Logging = new LoggingBuilder(Services);
            WebHost = _deferredWebHostBuilder = new ConfigureWebHostBuilder(Configuration, _environment, Services);
            Host = _deferredHostBuilder = new ConfigureHostBuilder(Configuration, _environment, Services);

            // Register Configuration as IConfiguration so updates can be observed even after the WebApplication is built.
            Services.AddSingleton<IConfiguration>(Configuration);

            // Add default services
            _deferredHostBuilder.ConfigureDefaults(args);

            // Enable changes here because we need to pick up configuration sources added by the generic web host
            _deferredHostBuilder.ConfigurationEnabled = true;
            _deferredHostBuilder.ConfigureWebHostDefaults(configure: _ => { });

            // This is important because GenericWebHostBuilder does the following and we want to preserve the WebHostBuilderContext:
            // context.Properties[typeof(WebHostBuilderContext)] = webHostBuilderContext;
            // context.Properties[typeof(WebHostOptions)] = options;
            foreach (var (key, value) in _deferredHostBuilder.Properties)
            {
                _hostBuilder.Properties[key] = value;
            }
        }

        /// <summary>
        /// Provides information about the web hosting environment an application is running.
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// A collection of services for the application to compose. This is useful for adding user provided or framework provided services.
        /// </summary>
        public IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// A collection of configuration providers for the application to compose. This is useful for adding new configuration sources and providers.
        /// </summary>
        public ConfigurationManager Configuration { get; } = new();

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
            // We call ConfigureWebHostDefaults AGAIN because config might be added like "ForwardedHeaders_Enabled"
            // which can add even more services. If not for that, we probably call _hostBuilder.ConfigureWebHost(ConfigureWebHost)
            // instead in order to avoid duplicate service registration.
            _hostBuilder.ConfigureWebHostDefaults(ConfigureWebHost);
            return _builtApplication = new WebApplication(_hostBuilder.Build());
        }

        private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app)
        {
            Debug.Assert(_builtApplication is not null);

            if (context.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // The endpoints were already added on the outside
            if (_builtApplication.DataSources.Count > 0)
            {
                // The user did not register the routing middleware so wrap the entire
                // destination pipeline in UseRouting() and UseEndpoints(), essentially:
                // destination.UseRouting()
                // destination.Run(source)
                // destination.UseEndpoints()
                if (_builtApplication.RouteBuilder == null)
                {
                    app.UseRouting();

                    // Copy the route data sources over to the destination pipeline, this should be available since we just called
                    // UseRouting()
                    var routes = (IEndpointRouteBuilder)app.Properties[WebApplication.EndpointRouteBuilder]!;

                    foreach (var ds in _builtApplication.DataSources)
                    {
                        routes.DataSources.Add(ds);
                    }

                    // Chain the execution of the source pipeline into the destination pipeline
                    app.Use(next =>
                    {
                        _builtApplication.Run(next);
                        return _builtApplication.BuildRequestDelegate();
                    });

                    // Add a UseEndpoints at the end
                    app.UseEndpoints(e => { });
                }
                else
                {
                    // Since we register routes into the source pipeline's route builder directly,
                    // if the user called UseRouting, we need to copy the data sources
                    foreach (var ds in _builtApplication.DataSources)
                    {
                        _builtApplication.RouteBuilder.DataSources.Add(ds);
                    }

                    // We then implicitly call UseEndpoints at the end of the pipeline
                    _builtApplication.UseEndpoints(_ => { });

                    // Wire the source pipeline to run in the destination pipeline
                    app.Run(_builtApplication.BuildRequestDelegate());
                }
            }
            else
            {
                // Wire the source pipeline to run in the destination pipeline
                app.Run(_builtApplication.BuildRequestDelegate());
            }

            // Copy the properties to the destination app builder
            foreach (var item in _builtApplication.Properties)
            {
                app.Properties[item.Key] = item.Value;
            }
        }

        private void ConfigureWebHost(IWebHostBuilder genericWebHostBuilder)
        {
            _hostBuilder.ConfigureHostConfiguration(builder =>
            {
                // All the sources in builder.Sources should be in Configuration.Sources
                // already thanks to the BootstrapHostBuilder.
                builder.Sources.Clear();

                foreach (var (key, value) in ((IConfigurationBuilder)Configuration).Properties)
                {
                    builder.Properties[key] = value;
                }

                builder.AddConfiguration(Configuration, shouldDisposeConfiguration: true);
            });

            genericWebHostBuilder.ConfigureServices((context, services) =>
            {
                // We've only added services configured by the GenericWebHostBuilder and WebHost.ConfigureWebDefaults
                // at this point. HostBuilder news up a new ServiceCollection in HostBuilder.Build() we haven't seen
                // until now, so we cannot clear these services even though some are redundant because
                // we called ConfigureWebHostDefaults on both the _deferredHostBuilder and _hostBuilder.

                // Ideally, we'd only call _hostBuilder.ConfigureWebHost(ConfigureWebHost) instead of
                // _hostBuilder.ConfigureWebHostDefaults(ConfigureWebHost) to avoid some duplicate service descriptors,
                // but we want to add services in the WebApplicationBuilder constructor so code can inspect
                // WebApplicationBuilder.Services. At the same time, we want to be able which services are loaded
                // to react to config changes (e.g. ForwardedHeadersStartupFilter).
                foreach (var s in Services)
                {
                    services.Add(s);
                }

                // Add any services to the user visible service collection so that they are observable
                // just in case users capture the Services property. Orchard does this to get a "blueprint"
                // of the service collection. The order needs to be preserved here so we clear the original
                // collection and add all of the services in order.
                Services.Clear();
                foreach (var s in services)
                {
                    Services.Add(s);
                }
            });

            genericWebHostBuilder.Configure(ConfigureApplication);

            _deferredHostBuilder.RunDeferredCallbacks(_hostBuilder);

            _environment.ApplyEnvironmentSettings(genericWebHostBuilder);
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

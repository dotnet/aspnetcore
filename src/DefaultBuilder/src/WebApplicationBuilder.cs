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
            Services = new ServiceCollection();

            // HACK: MVC and Identity do this horrible thing to get the hosting environment as an instance
            // from the service collection before it is built. That needs to be fixed...
            Environment = _environment = new WebHostEnvironment(callingAssembly);
            Services.AddSingleton(Environment);

            Configuration = new Configuration();

            // Run methods to configure both generic and web host defaults early to populate config from appsettings.json
            // environment variables (both DOTNET_ and ASPNETCORE_ prefixed) and other possible default sources to prepopulate
            // the correct defaults.
            var bootstrapBuilder = new BootstrapHostBuilder(Configuration, _environment);
            bootstrapBuilder.ConfigureDefaults(args);
            bootstrapBuilder.ConfigureWebHostDefaults(configure: _ => { });

            Configuration.SetBasePath(_environment.ContentRootPath);
            Logging = new LoggingBuilder(Services);
            WebHost = _deferredWebHostBuilder = new ConfigureWebHostBuilder(Configuration, _environment, Services);
            Host = _deferredHostBuilder = new ConfigureHostBuilder(Configuration, _environment, Services);

            _deferredHostBuilder.ConfigureDefaults(args);
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
        public Configuration Configuration { get; }

        /// <summary>
        /// A collection of logging providers for the applicaiton to compose. This is useful for adding new logging providers.
        /// </summary>
        public ILoggingBuilder Logging { get; }

        /// <summary>
        /// An <see cref="IHostBuilder"/> for configuring server specific properties, but not building.
        /// To build after configuruation, call <see cref="Build"/>.
        /// </summary>
        public ConfigureWebHostBuilder WebHost { get; }

        /// <summary>
        /// An <see cref="IWebHostBuilder"/> for configuring host specific properties, but not building.
        /// To build after configuration, call <see cref="Build"/>.
        /// </summary>
        public ConfigureHostBuilder Host { get; }

        /// <summary>
        /// Builds the <see cref="WebApplication"/>.
        /// </summary>
        /// <returns>A configured <see cref="WebApplication"/>.</returns>
        public WebApplication Build()
        {
            _hostBuilder.ConfigureWebHostDefaults(ConfigureWebHost);
            _builtApplication = new WebApplication(_hostBuilder.Build());
            return _builtApplication;
        }

        private void ConfigureApplication(WebHostBuilderContext context, IApplicationBuilder app)
        {
            Debug.Assert(_builtApplication is not null);

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
                        return _builtApplication.BuildRequstDelegate();
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
                    app.Run(_builtApplication.BuildRequstDelegate());
                }
            }
            else
            {
                // Wire the source pipeline to run in the destination pipeline
                app.Run(_builtApplication.BuildRequstDelegate());
            }

            // Copy the properties to the destination app builder
            foreach (var item in _builtApplication.Properties)
            {
                app.Properties[item.Key] = item.Value;
            }

        }

        private void ConfigureWebHost(IWebHostBuilder genericWebHostBuilder)
        {
            genericWebHostBuilder.Configure(ConfigureApplication);

            _hostBuilder.ConfigureServices((context, services) =>
            {
                foreach (var s in Services)
                {
                    services.Add(s);
                }
            });

            _hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                foreach (var s in Configuration.Sources)
                {
                    builder.Sources.Add(s);
                }
            });

            _deferredHostBuilder.ExecuteActions(_hostBuilder);
            _deferredWebHostBuilder.ExecuteActions(genericWebHostBuilder);

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        internal WebApplicationBuilder(Assembly? callingAssembly, string[]? args = null)
        {
            Services = new ServiceCollection();

            // HACK: MVC and Identity do this horrible thing to get the hosting environment as an instance
            // from the service collection before it is built. That needs to be fixed...
            Environment = _environment = new WebHostEnvironment(callingAssembly);
            Services.AddSingleton(Environment);

            Configuration = new Configuration();

            // Run this inline to populate the configuration
            new BootstrapHostBuilder(Configuration, Environment).ConfigureDefaults(args);

            Configuration.SetBasePath(_environment.ContentRootPath);
            Logging = new LoggingBuilder(Services);
            WebHost = _deferredWebHostBuilder = new ConfigureWebHostBuilder(Configuration, _environment, Services);
            Host = _deferredHostBuilder = new ConfigureHostBuilder(Configuration, _environment, Services, args);
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
            // This will always be set before Build completes or the ConfigureWebHostDefaults callback runs.
            WebApplication sourcePipeline = null!;

            _hostBuilder.ConfigureWebHostDefaults(web =>
            {
                web.Configure(destinationPipeline =>
                {
                    // The endpoints were already added on the outside
                    if (sourcePipeline.DataSources.Count > 0)
                    {
                        // The user did not register the routing middleware so wrap the entire
                        // destination pipeline in UseRouting() and UseEndpoints(), essentially:
                        // destination.UseRouting()
                        // destination.Run(source)
                        // destination.UseEndpoints()
                        if (sourcePipeline.RouteBuilder == null)
                        {
                            destinationPipeline.UseRouting();

                            // Copy the route data sources over to the destination pipeline, this should be available since we just called
                            // UseRouting()
                            var routes = (IEndpointRouteBuilder)destinationPipeline.Properties[WebApplication.EndpointRouteBuilder]!;

                            foreach (var ds in sourcePipeline.DataSources)
                            {
                                routes.DataSources.Add(ds);
                            }

                            // Chain the execution of the source pipeline into the destination pipeline
                            destinationPipeline.Use(next =>
                            {
                                sourcePipeline.Run(next);
                                return sourcePipeline.Build();
                            });

                            // Add a UseEndpoints at the end
                            destinationPipeline.UseEndpoints(e => { });
                        }
                        else
                        {
                            // Since we register routes into the source pipeline's route builder directly,
                            // if the user called UseRouting, we need to copy the data sources
                            foreach (var ds in sourcePipeline.DataSources)
                            {
                                sourcePipeline.RouteBuilder.DataSources.Add(ds);
                            }

                            // We then implicitly call UseEndpoints at the end of the pipeline
                            sourcePipeline.UseEndpoints(_ => { });

                            // Wire the source pipeline to run in the destination pipeline
                            destinationPipeline.Run(sourcePipeline.Build());
                        }
                    }
                    else
                    {
                        // Wire the source pipeline to run in the destination pipeline
                        destinationPipeline.Run(sourcePipeline.Build());
                    }

                    // Copy the properties to the destination app builder
                    foreach (var item in sourcePipeline.Properties)
                    {
                        destinationPipeline.Properties[item.Key] = item.Value;
                    }
                });

                _hostBuilder.ConfigureServices(services =>
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

                // Make the default web host settings match and allow overrides
                web.UseEnvironment(_environment.EnvironmentName);
                web.UseContentRoot(_environment.ContentRootPath);
                web.UseSetting(WebHostDefaults.ApplicationKey, _environment.ApplicationName);
                web.UseSetting(WebHostDefaults.WebRootKey, _environment.WebRootPath);

                _deferredWebHostBuilder.ExecuteActions(web);
            });

            var host = _hostBuilder.Build();

            return sourcePipeline = new WebApplication(host);
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

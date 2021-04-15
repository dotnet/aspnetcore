// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
        private readonly DeferredHostBuilder _deferredHostBuilder;
        private readonly DeferredWebHostBuilder _deferredWebHostBuilder;
        private readonly WebHostEnvironment _environment;

        /// <summary>
        /// Creates a <see cref="WebApplicationBuilder"/>.
        /// </summary>
        public WebApplicationBuilder() : this(callingAssembly: null, b => { })
        {

        }

        internal WebApplicationBuilder(Assembly? callingAssembly, Action<IHostBuilder> configureHost)
        {
            Services = new ServiceCollection();

            // HACK: MVC and Identity do this horrible thing to get the hosting environment as an instance
            // from the service collection before it is built. That needs to be fixed...
            Environment = _environment = new WebHostEnvironment(callingAssembly);
            Services.AddSingleton(Environment);

            Configuration = new Configuration();

            // Run this inline to populate the configuration
            configureHost(new ConfigurationHostBuilder(Configuration, Environment));

            Configuration.SetBasePath(_environment.ContentRootPath);
            Logging = new LoggingBuilder(Services);
            Server = _deferredWebHostBuilder = new DeferredWebHostBuilder(Configuration, _environment, Services);
            Host = _deferredHostBuilder = new DeferredHostBuilder(Configuration, configureHost, _environment, Services);
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
        /// A builder for configuring server specific properties. 
        /// </summary>
        public IWebHostBuilder Server { get; }

        /// <summary>
        /// A builder for configure host specific properties.
        /// </summary>
        public IHostBuilder Host { get; }

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

        private class DeferredHostBuilder : IHostBuilder
        {
            private Action<IHostBuilder>? _operations;

            public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

            private readonly IConfigurationBuilder _hostConfiguration = new ConfigurationBuilder();

            private readonly WebHostEnvironment _environment;
            private readonly Configuration _configuration;
            private readonly IServiceCollection _services;

            public DeferredHostBuilder(Configuration configuration, Action<IHostBuilder> configureHost, WebHostEnvironment environment, IServiceCollection services)
            {
                _configuration = configuration;
                _environment = environment;
                _services = services;

                configureHost(this);
            }

            public IHost Build()
            {
                throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
            }

            public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                _operations += b => b.ConfigureAppConfiguration(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
            {
                _operations += b => b.ConfigureContainer(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
            {
                // HACK: We need to evaluate the host configuration as they are changes so that we have an accurate view of the world
                configureDelegate(_hostConfiguration);

                var config = _hostConfiguration.Build();

                _environment.ApplicationName = config[HostDefaults.ApplicationKey] ?? _environment.ApplicationName;
                _environment.ContentRootPath = config[HostDefaults.ContentRootKey] ?? _environment.ContentRootPath;
                _environment.EnvironmentName = config[HostDefaults.EnvironmentKey] ?? _environment.EnvironmentName;
                _environment.ResolveFileProviders(config);
                _configuration.ChangeBasePath(_environment.ContentRootPath);

                _operations += b => b.ConfigureHostConfiguration(configureDelegate);
                return this;
            }

            public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
            {
                // Run these immediately so that they are observable by the imperative code
                configureDelegate(new HostBuilderContext(Properties)
                {
                    Configuration = _configuration,
                    HostingEnvironment = _environment
                },
                _services);

                return this;
            }

            public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
            {
                _operations += b => b.UseServiceProviderFactory(factory);
                return this;
            }

            public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
            {
                _operations += b => b.UseServiceProviderFactory(factory);
                return this;
            }

            public void ExecuteActions(IHostBuilder hostBuilder)
            {
                _operations?.Invoke(hostBuilder);
            }
        }

        private class DeferredWebHostBuilder : IWebHostBuilder
        {
            private Action<IWebHostBuilder>? _operations;

            private readonly WebHostEnvironment _environment;
            private readonly Configuration _configuration;
            private readonly Dictionary<string, string?> _settings = new Dictionary<string, string?>();
            private readonly IServiceCollection _services;

            public DeferredWebHostBuilder(Configuration configuration, WebHostEnvironment environment, IServiceCollection services)
            {
                _configuration = configuration;
                _environment = environment;
                _services = services;
            }

            IWebHost IWebHostBuilder.Build()
            {
                throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
            }

            public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                _operations += b => b.ConfigureAppConfiguration(configureDelegate);
                return this;
            }

            public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
            {
                configureServices(new WebHostBuilderContext
                {
                    Configuration = _configuration,
                    HostingEnvironment = _environment
                },
                _services);
                return this;
            }

            public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
            {
                return ConfigureServices((WebHostBuilderContext context, IServiceCollection services) => configureServices(services));
            }

            public string? GetSetting(string key)
            {
                _settings.TryGetValue(key, out var value);
                return value;
            }

            public IWebHostBuilder UseSetting(string key, string? value)
            {
                _settings[key] = value;
                _operations += b => b.UseSetting(key, value);

                // All preoperties on IWebHostEnvironment are non-nullable.
                if (value is null)
                {
                    return this;
                }

                if (key == WebHostDefaults.ApplicationKey)
                {
                    _environment.ApplicationName = value;
                }
                else if (key == WebHostDefaults.ContentRootKey)
                {
                    _environment.ContentRootPath = value;
                    _environment.ResolveFileProviders(_configuration);

                    _configuration.ChangeBasePath(value);
                }
                else if (key == WebHostDefaults.EnvironmentKey)
                {
                    _environment.EnvironmentName = value;
                }
                else if (key == WebHostDefaults.WebRootKey)
                {
                    _environment.WebRootPath = value;
                    _environment.ResolveFileProviders(_configuration);
                }

                return this;
            }

            public void ExecuteActions(IWebHostBuilder webHostBuilder)
            {
                _operations?.Invoke(webHostBuilder);
            }
        }

        private class LoggingBuilder : ILoggingBuilder
        {
            public LoggingBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }

        private class WebHostEnvironment : IWebHostEnvironment
        {
            private static readonly NullFileProvider NullFileProvider = new();

            public WebHostEnvironment(Assembly? callingAssembly)
            {
                ContentRootPath = Directory.GetCurrentDirectory();

                ApplicationName = (callingAssembly ?? Assembly.GetEntryAssembly())?.GetName()?.Name ?? "NotFound";
                EnvironmentName = Environments.Production;

                // This feels wrong, but HostingEnvironment does the same thing.
                WebRootPath = default!;

                // Default to /wwwroot if it exists.
                var wwwroot = Path.Combine(ContentRootPath, "wwwroot");
                if (Directory.Exists(wwwroot))
                {
                    WebRootPath = wwwroot;
                }

                ContentRootFileProvider = NullFileProvider;
                WebRootFileProvider = NullFileProvider;

                ResolveFileProviders(new Configuration());
            }

            public void ResolveFileProviders(IConfiguration configuration)
            {
                if (Directory.Exists(ContentRootPath))
                {
                    ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
                }

                if (Directory.Exists(WebRootPath))
                {
                    WebRootFileProvider = new PhysicalFileProvider(Path.Combine(ContentRootPath, WebRootPath));
                }

                if (this.IsDevelopment())
                {
                    StaticWebAssetsLoader.UseStaticWebAssets(this, configuration);
                }
            }

            public string ApplicationName { get; set; }
            public string EnvironmentName { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }

            public IFileProvider WebRootFileProvider { get; set; }

            public string WebRootPath { get; set; }
        }
    }
}
